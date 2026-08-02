using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FluidBackground.Core.Models;
using FluidBackground.Core.Renderers;
using SkiaSharp;
using System.Diagnostics;

namespace FluidBackground.Avalonia11;

/// <summary>
/// Avalonia平台的闪电电弧进度边界控件
/// <para>
/// 渲染一张深色圆角卡片，内部有一条随进度移动的闪电状发光边界。
/// 支持拖拽调节进度、4 种内置主题与完整动画行为（自然抖动、能量残留、充能完成脉冲等）。
/// </para>
/// </summary>
public class LightningArcProgressControl : Control
{
    private LightningArcRenderer? _renderer;
    private readonly Stopwatch _stopwatch = new();
    private DispatcherTimer? _animationTimer;
    private WriteableBitmap? _writeableBitmap;
    private bool _isDragging;

    /// <summary>
    /// 闪电电弧配置属性
    /// </summary>
    public static readonly StyledProperty<LightningArcConfig?> ConfigProperty =
        AvaloniaProperty.Register<LightningArcProgressControl, LightningArcConfig?>(
            nameof(Config),
            defaultValue: null);

    /// <summary>
    /// 进度属性（0-100）
    /// </summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<LightningArcProgressControl, double>(
            nameof(Progress),
            defaultValue: 30d);

    /// <summary>
    /// 是否启用动画
    /// </summary>
    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<LightningArcProgressControl, bool>(
            nameof(IsAnimationEnabled),
            defaultValue: true);

    /// <summary>
    /// 帧率限制（0表示不限制）
    /// </summary>
    public static readonly StyledProperty<int> MaxFpsProperty =
        AvaloniaProperty.Register<LightningArcProgressControl, int>(
            nameof(MaxFps),
            defaultValue: 60);

    /// <summary>
    /// 是否允许拖拽调节进度
    /// </summary>
    public static readonly StyledProperty<bool> EnableDragInteractionProperty =
        AvaloniaProperty.Register<LightningArcProgressControl, bool>(
            nameof(EnableDragInteraction),
            defaultValue: true);

    /// <summary>
    /// 闪电电弧配置
    /// </summary>
    public LightningArcConfig? Config
    {
        get => GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    /// <summary>
    /// 进度（0-100）
    /// </summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
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

    /// <summary>
    /// 是否允许拖拽调节进度
    /// </summary>
    public bool EnableDragInteraction
    {
        get => GetValue(EnableDragInteractionProperty);
        set => SetValue(EnableDragInteractionProperty, value);
    }

    static LightningArcProgressControl()
    {
        AffectsRender<LightningArcProgressControl>(ConfigProperty, ProgressProperty, IsAnimationEnabledProperty);
    }

    public LightningArcProgressControl()
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
            if (_renderer != null)
            {
                var config = change.NewValue as LightningArcConfig ?? new LightningArcConfig();
                _renderer.UpdateConfig(config);
                _renderer.SetTargetProgress(config.Progress);
                InvalidateVisual();
            }
        }
        else if (change.Property == ProgressProperty)
        {
            if (_renderer != null && change.NewValue is double progress)
            {
                _renderer.SetTargetProgress((float)(progress / 100d));
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
        var config = Config ?? new LightningArcConfig();
        _renderer = new LightningArcRenderer(config);
        _renderer.SetTargetProgress((float)(Progress / 100d));
        _stopwatch.Start();
    }

    private void CleanupRenderer()
    {
        _renderer?.Dispose();
        _renderer = null;
        _writeableBitmap?.Dispose();
        _writeableBitmap = null;
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
        if (_renderer == null)
            return;

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_renderer == null)
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

        using var skBitmap = _renderer.RenderToBitmap(time, width, height);

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
            var srcPtr = skBitmap.GetPixels();
            var dstPtr = fb.Address;
            var srcRowBytes = skBitmap.RowBytes;
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

    // ==================== 拖拽交互 ====================

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_renderer == null || !EnableDragInteraction || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        _isDragging = true;
        e.Pointer.Capture(this);
        _renderer.SetDragging(true);
        UpdateProgressFromPosition(e);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging || _renderer == null)
            return;

        UpdateProgressFromPosition(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isDragging)
            return;

        _isDragging = false;
        e.Pointer.Capture(null);
        _renderer?.SetDragging(false);
        e.Handled = true;
    }

    private void UpdateProgressFromPosition(PointerEventArgs e)
    {
        var width = Bounds.Width;
        if (width <= 0)
            return;

        var x = e.GetPosition(this).X;
        var value = Math.Clamp(x / width * 100d, 0d, 100d);
        Progress = value;
    }
}
