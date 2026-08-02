using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FluidBackground.Core.Models;
using FluidBackground.Core.Renderers;
using SkiaSharp;

namespace FluidBackground.WPF;

/// <summary>
/// WPF平台的闪电电弧进度边界控件
/// <para>
/// 渲染一张深色圆角卡片，内部有一条随进度移动的闪电状发光边界。
/// 支持拖拽调节进度、4 种内置主题与完整动画行为（自然抖动、能量残留、充能完成脉冲等）。
/// </para>
/// </summary>
public class LightningArcProgressControl : FrameworkElement
{
    private LightningArcRenderer? _renderer;
    private readonly Stopwatch _stopwatch = new();
    private bool _isAnimating;
    private double _lastRenderTime;
    private WriteableBitmap? _writeableBitmap;
    private SKBitmap? _cachedBitmap;
    private bool _isDragging;

    /// <summary>
    /// 配置依赖属性
    /// </summary>
    public static readonly DependencyProperty ConfigProperty =
        DependencyProperty.Register(
            nameof(Config),
            typeof(LightningArcConfig),
            typeof(LightningArcProgressControl),
            new PropertyMetadata(null, OnConfigChanged));

    /// <summary>
    /// 进度依赖属性（0-100）
    /// </summary>
    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(LightningArcProgressControl),
            new PropertyMetadata(30d, OnProgressChanged));

    /// <summary>
    /// 是否启用动画依赖属性
    /// </summary>
    public static readonly DependencyProperty IsAnimationEnabledProperty =
        DependencyProperty.Register(
            nameof(IsAnimationEnabled),
            typeof(bool),
            typeof(LightningArcProgressControl),
            new PropertyMetadata(true, OnIsAnimationEnabledChanged));

    /// <summary>
    /// 帧率限制依赖属性
    /// </summary>
    public static readonly DependencyProperty MaxFpsProperty =
        DependencyProperty.Register(
            nameof(MaxFps),
            typeof(int),
            typeof(LightningArcProgressControl),
            new PropertyMetadata(60));

    /// <summary>
    /// 是否允许拖拽调节进度依赖属性
    /// </summary>
    public static readonly DependencyProperty EnableDragInteractionProperty =
        DependencyProperty.Register(
            nameof(EnableDragInteraction),
            typeof(bool),
            typeof(LightningArcProgressControl),
            new PropertyMetadata(true));

    /// <summary>
    /// 闪电电弧配置（null 时使用默认配置）
    /// </summary>
    public LightningArcConfig? Config
    {
        get => (LightningArcConfig?)GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    /// <summary>
    /// 进度（0-100）
    /// </summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>
    /// 是否启用动画
    /// </summary>
    public bool IsAnimationEnabled
    {
        get => (bool)GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    /// <summary>
    /// 帧率限制
    /// </summary>
    public int MaxFps
    {
        get => (int)GetValue(MaxFpsProperty);
        set => SetValue(MaxFpsProperty, value);
    }

    /// <summary>
    /// 是否允许拖拽调节进度
    /// </summary>
    public bool EnableDragInteraction
    {
        get => (bool)GetValue(EnableDragInteractionProperty);
        set => SetValue(EnableDragInteractionProperty, value);
    }

    /// <summary>
    /// 当前显示进度（0-100，弹性插值后的值，供外部 UI 显示）
    /// </summary>
    public double DisplayProgress => _renderer?.DisplayProgress * 100d ?? Progress;

    public LightningArcProgressControl()
    {
        ClipToBounds = true;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not LightningArcProgressControl control)
            return;

        if (control._renderer != null)
        {
            var config = e.NewValue as LightningArcConfig ?? new LightningArcConfig();
            // UpdateConfig 内部会同步目标进度（config.Progress）
            control._renderer.UpdateConfig(config);
            control.InvalidateVisual();
        }
    }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LightningArcProgressControl control && control._renderer != null)
        {
            control._renderer.SetTargetProgress((float)((double)e.NewValue / 100d));
            control.InvalidateVisual();
        }
    }

    private static void OnIsAnimationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LightningArcProgressControl control)
        {
            if ((bool)e.NewValue)
                control.StartAnimation();
            else
                control.StopAnimation();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeRenderer();
        if (IsAnimationEnabled)
            StartAnimation();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopAnimation();
        CleanupRenderer();
    }

    private void InitializeRenderer()
    {
        var config = Config ?? new LightningArcConfig();
        _renderer = new LightningArcRenderer(config);
        _renderer.SetTargetProgress((float)(Progress / 100d));
        _stopwatch.Start();
    }

    private void CleanupRenderer()
    {
        _renderer?.Dispose();
        _renderer = null;
        _writeableBitmap = null;
        _cachedBitmap?.Dispose();
        _cachedBitmap = null;
    }

    private void StartAnimation()
    {
        if (_isAnimating)
            return;

        _isAnimating = true;
        _stopwatch.Restart();
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopAnimation()
    {
        if (!_isAnimating)
            return;

        _isAnimating = false;
        CompositionTarget.Rendering -= OnRendering;
        _stopwatch.Stop();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isAnimating || _renderer == null)
            return;

        var currentTime = _stopwatch.Elapsed.TotalSeconds;

        if (MaxFps > 0)
        {
            var minInterval = 1.0 / MaxFps;
            if (currentTime - _lastRenderTime < minInterval)
                return;
        }

        _lastRenderTime = currentTime;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_renderer == null)
        {
            return;
        }

        var width = (int)ActualWidth;
        var height = (int)ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        var time = _stopwatch.Elapsed.TotalSeconds;

        // 复用 SKBitmap
        if (_cachedBitmap == null || _cachedBitmap.Width != width || _cachedBitmap.Height != height)
        {
            _cachedBitmap?.Dispose();
            _cachedBitmap = _renderer.RenderToBitmap(time, width, height);
        }
        else
        {
            _renderer.RenderToBitmap(time, width, height, _cachedBitmap);
        }

        if (_writeableBitmap == null ||
            _writeableBitmap.PixelWidth != width ||
            _writeableBitmap.PixelHeight != height)
        {
            _writeableBitmap = new WriteableBitmap(
                width, height, 96, 96,
                PixelFormats.Pbgra32, null);
        }

        _writeableBitmap.Lock();
        CopySKBitmapToWriteableBitmap(_cachedBitmap, _writeableBitmap);
        _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        _writeableBitmap.Unlock();

        drawingContext.DrawImage(_writeableBitmap, new Rect(0, 0, width, height));
    }

    private static unsafe void CopySKBitmapToWriteableBitmap(SKBitmap source, WriteableBitmap target)
    {
        var sourceData = source.GetPixelSpan();
        var targetBuffer = target.BackBuffer;
        var stride = target.BackBufferStride;
        var width = source.Width;
        var height = source.Height;

        fixed (byte* srcPtr = sourceData)
        {
            var src = srcPtr;
            var dst = (byte*)targetBuffer;

            for (int y = 0; y < height; y++)
            {
                var srcRow = src + y * width * 4;
                var dstRow = dst + y * stride;

                for (int x = 0; x < width; x++)
                {
                    var offset = x * 4;
                    dstRow[offset] = srcRow[offset + 2];     // B
                    dstRow[offset + 1] = srcRow[offset + 1]; // G
                    dstRow[offset + 2] = srcRow[offset];     // R
                    dstRow[offset + 3] = srcRow[offset + 3]; // A
                }
            }
        }
    }

    // ==================== 拖拽交互 ====================

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (_renderer == null || !EnableDragInteraction)
            return;

        _isDragging = true;
        CaptureMouse();
        _renderer.SetDragging(true);
        UpdateProgressFromPosition(e);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isDragging || _renderer == null)
            return;

        UpdateProgressFromPosition(e);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_isDragging)
            return;

        _isDragging = false;
        ReleaseMouseCapture();
        _renderer?.SetDragging(false);
        e.Handled = true;
    }

    private void UpdateProgressFromPosition(MouseEventArgs e)
    {
        var width = ActualWidth;
        if (width <= 0)
            return;

        var x = e.GetPosition(this).X;
        var value = Math.Clamp(x / width * 100d, 0d, 100d);
        SetValue(ProgressProperty, value);
        _renderer?.SetTargetProgress((float)(value / 100d));
    }
}
