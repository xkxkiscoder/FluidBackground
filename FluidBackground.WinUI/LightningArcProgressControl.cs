using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using FluidBackground.Core.Models;
using FluidBackground.Core.Renderers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Windows.Graphics.Imaging;

namespace FluidBackground.WinUI;

/// <summary>
/// WinUI 3平台的闪电电弧进度边界控件
/// <para>
/// 渲染一张深色圆角卡片，内部有一条随进度移动的闪电状发光边界。
/// 支持拖拽调节进度、4 种内置主题与完整动画行为（自然抖动、能量残留、充能完成脉冲等）。
/// </para>
/// </summary>
public class LightningArcProgressControl : ContentControl
{
    private LightningArcRenderer? _renderer;
    private readonly Stopwatch _stopwatch = new();
    private bool _isAnimating;
    private bool _isDragging;
    private bool _framePending;
    private SKBitmap? _cachedBitmap;
    private SoftwareBitmapSource? _bitmapSource;

    public static readonly DependencyProperty ConfigProperty =
        DependencyProperty.Register(nameof(Config), typeof(LightningArcConfig), typeof(LightningArcProgressControl),
            new PropertyMetadata(null, OnConfigChanged));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(LightningArcProgressControl),
            new PropertyMetadata(30d, OnProgressChanged));

    public static readonly DependencyProperty IsAnimationEnabledProperty =
        DependencyProperty.Register(nameof(IsAnimationEnabled), typeof(bool), typeof(LightningArcProgressControl),
            new PropertyMetadata(true, OnIsAnimationEnabledChanged));

    public static readonly DependencyProperty MaxFpsProperty =
        DependencyProperty.Register(nameof(MaxFps), typeof(int), typeof(LightningArcProgressControl),
            new PropertyMetadata(60));

    public static readonly DependencyProperty EnableDragInteractionProperty =
        DependencyProperty.Register(nameof(EnableDragInteraction), typeof(bool), typeof(LightningArcProgressControl),
            new PropertyMetadata(true));

    /// <summary>
    /// 闪电电弧配置
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
    /// 当前显示进度（0-100，弹性插值后的值，供外部 UI 显示）
    /// </summary>
    public double DisplayProgress => _renderer?.DisplayProgress * 100d ?? Progress;

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

    public LightningArcProgressControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LightningArcProgressControl control && control._renderer != null)
        {
            var config = e.NewValue as LightningArcConfig ?? new LightningArcConfig();
            // UpdateConfig 内部已同步目标进度（config.Progress）
            control._renderer.UpdateConfig(config);
            control.RenderFrame();
        }
    }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LightningArcProgressControl control && control._renderer != null)
        {
            control._renderer.SetTargetProgress((float)((double)e.NewValue / 100d));
            control.RenderFrame();
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
        _cachedBitmap?.Dispose();
        _cachedBitmap = null;
        _bitmapSource?.Dispose();
        _bitmapSource = null;
    }

    private void StartAnimation()
    {
        if (_isAnimating) return;
        _isAnimating = true;
        _stopwatch.Restart();
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopAnimation()
    {
        if (!_isAnimating) return;
        _isAnimating = false;
        CompositionTarget.Rendering -= OnRendering;
        _stopwatch.Stop();
    }

    private void OnRendering(object? sender, object e)
    {
        RenderFrame();
    }

    private async void RenderFrame()
    {
        if (_renderer == null || _framePending)
            return;

        var width = (int)ActualWidth;
        var height = (int)ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        var time = _stopwatch.Elapsed.TotalSeconds;

        if (_cachedBitmap == null || _cachedBitmap.Width != width || _cachedBitmap.Height != height)
        {
            _cachedBitmap?.Dispose();
            _cachedBitmap = _renderer.RenderToBitmap(time, width, height);
        }
        else
        {
            _renderer.RenderToBitmap(time, width, height, _cachedBitmap);
        }

        // 转换为 SoftwareBitmap 并显示（上一帧异步完成期间丢帧，避免写同一 bitmap）
        var softwareBitmap = ConvertToSoftwareBitmap(_cachedBitmap);

        if (_bitmapSource == null)
        {
            _bitmapSource = new SoftwareBitmapSource();
            var image = new Image
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Fill
            };
            image.Source = _bitmapSource;
            Content = image;
        }

        _framePending = true;
        try
        {
            await _bitmapSource.SetBitmapAsync(softwareBitmap);
        }
        catch (Exception)
        {
            // 显示管线异常不向上传播（避免击穿 UI 线程），下一帧重试
        }
        finally
        {
            softwareBitmap.Dispose();
            _framePending = false;
        }
    }

    private static unsafe SoftwareBitmap ConvertToSoftwareBitmap(SKBitmap source)
    {
        var width = source.Width;
        var height = source.Height;
        var pixelData = source.GetPixelSpan();

        var bitmap = new SoftwareBitmap(
            BitmapPixelFormat.Bgra8,
            width,
            height,
            BitmapAlphaMode.Premultiplied);

        // RGBA -> BGRA 转换
        var buffer = new byte[pixelData.Length];
        for (int i = 0; i < pixelData.Length; i += 4)
        {
            buffer[i] = pixelData[i + 2];     // B
            buffer[i + 1] = pixelData[i + 1]; // G
            buffer[i + 2] = pixelData[i];     // R
            buffer[i + 3] = pixelData[i + 3]; // A
        }
        bitmap.CopyFromBuffer(buffer.AsBuffer());

        return bitmap;
    }

    // ==================== 拖拽交互 ====================

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_renderer == null || !EnableDragInteraction || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        _isDragging = true;
        _renderer.SetDragging(true);
        UpdateProgressFromPosition(e);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging || _renderer == null)
            return;

        UpdateProgressFromPosition(e);
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isDragging)
            return;

        _isDragging = false;
        _renderer?.SetDragging(false);
        e.Handled = true;
    }

    private void UpdateProgressFromPosition(PointerRoutedEventArgs e)
    {
        var width = ActualWidth;
        if (width <= 0)
            return;

        var x = e.GetCurrentPoint(this).Position.X;
        var value = Math.Clamp(x / width * 100d, 0d, 100d);
        Progress = value;
    }
}
