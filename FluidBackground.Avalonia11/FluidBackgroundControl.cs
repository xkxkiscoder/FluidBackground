using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.Threading;
using FluidBackground.Core;
using FluidBackground.Core.Models;
using SkiaSharp;
using System.Diagnostics;

namespace FluidBackground.Avalonia11;

/// <summary>
/// Avalonia平台的流体背景控件
/// </summary>
public class FluidBackgroundControl : Control
{
    private FluidRenderer? _renderer;
    private readonly Stopwatch _stopwatch = new();
    private DispatcherTimer? _animationTimer;
    private WriteableBitmap? _writeableBitmap;
    private SKBitmap? _cachedBitmap;
    private int _lastWidth;
    private int _lastHeight;

    /// <summary>
    /// 流体配置属性
    /// </summary>
    public static readonly StyledProperty<FluidConfig?> ConfigProperty =
        AvaloniaProperty.Register<FluidBackgroundControl, FluidConfig?>(
            nameof(Config),
            defaultValue: null);

    /// <summary>
    /// 是否启用动画
    /// </summary>
    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<FluidBackgroundControl, bool>(
            nameof(IsAnimationEnabled),
            defaultValue: true);

    /// <summary>
    /// 帧率限制（0表示不限制）
    /// </summary>
    public static readonly StyledProperty<int> MaxFpsProperty =
        AvaloniaProperty.Register<FluidBackgroundControl, int>(
            nameof(MaxFps),
            defaultValue: 60);

    /// <summary>
    /// 流体配置
    /// </summary>
    public FluidConfig? Config
    {
        get => GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    /// <summary>
    /// 是否启用动画
    /// </summary>
    public bool IsAnimationEnabled
    {
        get => GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    /// <summary>
    /// 帧率限制
    /// </summary>
    public int MaxFps
    {
        get => GetValue(MaxFpsProperty);
        set => SetValue(MaxFpsProperty, value);
    }

    static FluidBackgroundControl()
    {
        AffectsRender<FluidBackgroundControl>(ConfigProperty, IsAnimationEnabledProperty);
    }

    public FluidBackgroundControl()
    {
        ClipToBounds = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InitializeRenderer();
        if (IsAnimationEnabled)
            StartAnimation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        StopAnimation();
        CleanupRenderer();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ConfigProperty)
        {
            if (_renderer != null && change.NewValue is FluidConfig config)
            {
                _renderer.UpdateConfig(config);
                InvalidateVisual();
            }
        }
        else if (change.Property == IsAnimationEnabledProperty)
        {
            if (change.NewValue is true)
                StartAnimation();
            else
                StopAnimation();
        }
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
        _writeableBitmap?.Dispose();
        _writeableBitmap = null;
        _cachedBitmap?.Dispose();
        _cachedBitmap = null;
    }

    private void StartAnimation()
    {
        if (_animationTimer != null)
            return;

        var interval = MaxFps > 0 ? TimeSpan.FromMilliseconds(1000.0 / MaxFps) : TimeSpan.FromMilliseconds(16);

        _animationTimer = new DispatcherTimer
        {
            Interval = interval
        };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
        _stopwatch.Restart();
    }

    private void StopAnimation()
    {
        if (_animationTimer == null)
            return;

        _animationTimer.Stop();
        _animationTimer.Tick -= OnAnimationTick;
        _animationTimer = null;
        _stopwatch.Stop();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (_renderer == null || !_renderer.IsAvailable)
            return;

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_renderer == null || !_renderer.IsAvailable)
        {
            base.Render(context);
            return;
        }

        var bounds = Bounds;
        var width = (int)bounds.Width;
        var height = (int)bounds.Height;

        if (width <= 0 || height <= 0)
            return;

        var time = _stopwatch.Elapsed.TotalSeconds;

        // 复用 SKBitmap 缓冲区，避免每帧分配
        if (_cachedBitmap == null || _cachedBitmap.Width != width || _cachedBitmap.Height != height)
        {
            _cachedBitmap?.Dispose();
            _cachedBitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            _lastWidth = width;
            _lastHeight = height;
        }

        _renderer.RenderToBitmap(time, width, height, _cachedBitmap);

        if (_writeableBitmap == null ||
            _writeableBitmap.PixelSize.Width != width ||
            _writeableBitmap.PixelSize.Height != height)
        {
            _writeableBitmap?.Dispose();
            _writeableBitmap = new WriteableBitmap(
                new PixelSize(width, height),
                new global::Avalonia.Vector(96, 96),
                PixelFormats.Rgba8888,
                AlphaFormat.Premul);
        }

        using (var fb = _writeableBitmap.Lock())
        {
            var srcPtr = _cachedBitmap.GetPixels();
            var dstPtr = fb.Address;
            var srcRowBytes = _cachedBitmap.RowBytes;
            var dstRowBytes = fb.RowBytes;

            unsafe
            {
                for (int y = 0; y < height; y++)
                {
                    var srcRow = (byte*)srcPtr + y * srcRowBytes;
                    var dstRow = (byte*)dstPtr + y * dstRowBytes;
                    Buffer.MemoryCopy(srcRow, dstRow, dstRowBytes, Math.Min(srcRowBytes, dstRowBytes));
                }
            }
        }

        context.DrawImage(_writeableBitmap, new Rect(0, 0, width, height));
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_renderer == null || Config?.EnablePointerInteraction != true)
            return;

        var position = e.GetPosition(this);
        var bounds = Bounds;

        if (bounds.Width > 0 && bounds.Height > 0)
        {
            var x = (float)(position.X / bounds.Width);
            var y = (float)(position.Y / bounds.Height);
            _renderer.SetPointerPosition(x, y);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        UpdatePointerPosition(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        UpdatePointerPosition(e);
    }

    private void UpdatePointerPosition(PointerEventArgs e)
    {
        if (_renderer == null || Config?.EnablePointerInteraction != true)
            return;

        var position = e.GetPosition(this);
        var bounds = Bounds;

        if (bounds.Width > 0 && bounds.Height > 0)
        {
            var x = (float)(position.X / bounds.Width);
            var y = (float)(position.Y / bounds.Height);
            _renderer.SetPointerPosition(x, y);
        }
    }
}
