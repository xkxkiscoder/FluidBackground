using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using FluidBackground.Core;
using FluidBackground.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Windows.Graphics.Imaging;

namespace FluidBackground.WinUI;

/// <summary>
/// WinUI 3平台的流体背景控件
/// </summary>
public class FluidBackgroundControl : ContentControl
{
    private FluidRenderer? _renderer;
    private readonly Stopwatch _stopwatch = new();
    private bool _isAnimating;
    private SKBitmap? _cachedBitmap;
    private SoftwareBitmapSource? _bitmapSource;

    public static readonly DependencyProperty ConfigProperty =
        DependencyProperty.Register(nameof(Config), typeof(FluidConfig), typeof(FluidBackgroundControl),
            new PropertyMetadata(null, OnConfigChanged));

    public static readonly DependencyProperty IsAnimationEnabledProperty =
        DependencyProperty.Register(nameof(IsAnimationEnabled), typeof(bool), typeof(FluidBackgroundControl),
            new PropertyMetadata(true, OnIsAnimationEnabledChanged));

    public static readonly DependencyProperty MaxFpsProperty =
        DependencyProperty.Register(nameof(MaxFps), typeof(int), typeof(FluidBackgroundControl),
            new PropertyMetadata(60));

    public FluidConfig? Config
    {
        get => (FluidConfig?)GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    public bool IsAnimationEnabled
    {
        get => (bool)GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    public int MaxFps
    {
        get => (int)GetValue(MaxFpsProperty);
        set => SetValue(MaxFpsProperty, value);
    }

    public FluidBackgroundControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FluidBackgroundControl control && control._renderer != null)
        {
            var config = e.NewValue as FluidConfig ?? new FluidConfig();
            control._renderer.UpdateConfig(config);
            control.RenderFrame();
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

    private void RenderFrame()
    {
        if (_renderer == null || !_renderer.IsAvailable)
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

        // 转换为 SoftwareBitmap 并显示
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

        _bitmapSource.SetBitmapAsync(softwareBitmap);
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

    protected override void OnPointerMoved(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_renderer == null || Config?.EnablePointerInteraction != true) return;

        var position = e.GetCurrentPoint(this).Position;
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
