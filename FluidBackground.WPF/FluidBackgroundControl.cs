using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FluidBackground.Core;
using FluidBackground.Core.Models;
using SkiaSharp;

namespace FluidBackground.WPF;

/// <summary>
/// WPF平台的流体背景控件
/// </summary>
public class FluidBackgroundControl : FrameworkElement
{
    private FluidRenderer? _renderer;
    private readonly Stopwatch _stopwatch = new();
    private bool _isAnimating;
    private double _lastRenderTime;
    private WriteableBitmap? _writeableBitmap;

    /// <summary>
    /// 流体配置依赖属性
    /// </summary>
    public static readonly DependencyProperty ConfigProperty =
        DependencyProperty.Register(
            nameof(Config),
            typeof(FluidConfig),
            typeof(FluidBackgroundControl),
            new PropertyMetadata(null, OnConfigChanged));

    /// <summary>
    /// 是否启用动画依赖属性
    /// </summary>
    public static readonly DependencyProperty IsAnimationEnabledProperty =
        DependencyProperty.Register(
            nameof(IsAnimationEnabled),
            typeof(bool),
            typeof(FluidBackgroundControl),
            new PropertyMetadata(true, OnIsAnimationEnabledChanged));

    /// <summary>
    /// 帧率限制依赖属性
    /// </summary>
    public static readonly DependencyProperty MaxFpsProperty =
        DependencyProperty.Register(
            nameof(MaxFps),
            typeof(int),
            typeof(FluidBackgroundControl),
            new PropertyMetadata(60));

    /// <summary>
    /// 流体配置
    /// </summary>
    public FluidConfig? Config
    {
        get => (FluidConfig?)GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
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

    public FluidBackgroundControl()
    {
        ClipToBounds = true;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FluidBackgroundControl control && control._renderer != null)
        {
            var config = e.NewValue as FluidConfig ?? new FluidConfig();
            control._renderer.UpdateConfig(config);
            control.InvalidateVisual();
        }
    }

    private static void OnIsAnimationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FluidBackgroundControl control)
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
        var config = Config ?? new FluidConfig();
        _renderer = FluidRenderer.Create(config);
        _stopwatch.Start();
    }

    private void CleanupRenderer()
    {
        _renderer?.Dispose();
        _renderer = null;
        _writeableBitmap = null;
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
        if (_renderer == null || !_renderer.IsAvailable)
        {
            base.OnRender(drawingContext);
            return;
        }

        var width = (int)ActualWidth;
        var height = (int)ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        var time = _stopwatch.Elapsed.TotalSeconds;

        using var bitmap = _renderer.RenderToBitmap(time, width, height);

        if (_writeableBitmap == null ||
            _writeableBitmap.PixelWidth != width ||
            _writeableBitmap.PixelHeight != height)
        {
            _writeableBitmap = new WriteableBitmap(
                width, height, 96, 96,
                PixelFormats.Pbgra32, null);
        }

        _writeableBitmap.Lock();
        CopySKBitmapToWriteableBitmap(bitmap, _writeableBitmap);
        _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        _writeableBitmap.Unlock();

        drawingContext.DrawImage(_writeableBitmap, new Rect(0, 0, width, height));
    }

    private static void CopySKBitmapToWriteableBitmap(SKBitmap source, WriteableBitmap target)
    {
        var sourceData = source.GetPixelSpan();
        var targetBuffer = target.BackBuffer;
        var stride = target.BackBufferStride;

        unsafe
        {
            fixed (byte* srcPtr = sourceData)
            {
                var src = srcPtr;
                var dst = (byte*)targetBuffer;

                for (int y = 0; y < source.Height; y++)
                {
                    for (int x = 0; x < source.Width; x++)
                    {
                        var srcOffset = (y * source.Width + x) * 4;
                        var dstOffset = y * stride + x * 4;

                        dst[dstOffset + 0] = src[srcOffset + 2]; // B
                        dst[dstOffset + 1] = src[srcOffset + 1]; // G
                        dst[dstOffset + 2] = src[srcOffset + 0]; // R
                        dst[dstOffset + 3] = src[srcOffset + 3]; // A
                    }
                }
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_renderer == null || Config?.EnablePointerInteraction != true)
            return;

        var position = e.GetPosition(this);
        var width = ActualWidth;
        var height = ActualHeight;

        if (width > 0 && height > 0)
        {
            var x = (float)(position.X / width);
            var y = (float)(position.Y / height);
            _renderer.SetPointerPosition(x, y);
        }
    }
}
