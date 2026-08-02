using System.Diagnostics;
using FluidBackground.Core.Models;
using FluidBackground.Core.Renderers;
using SkiaSharp;

namespace FluidBackground.WinForms;

/// <summary>
/// WinForms平台的闪电电弧进度边界控件
/// <para>
/// 渲染一张深色圆角卡片，内部有一条随进度移动的闪电状发光边界。
/// 支持拖拽调节进度、4 种内置主题与完整动画行为（自然抖动、能量残留、充能完成脉冲等）。
/// </para>
/// </summary>
public class LightningArcProgressControl : Control
{
    private LightningArcRenderer? _renderer;
    private readonly Stopwatch _stopwatch = new();
    private System.Windows.Forms.Timer? _animationTimer;
    private SKBitmap? _cachedBitmap;
    private bool _disposed;
    private bool _isDragging;

    private LightningArcConfig? _config;
    private double _progress = 30;
    private bool _isAnimationEnabled = true;
    private int _maxFps = 60;
    private bool _enableDragInteraction = true;

    /// <summary>
    /// 闪电电弧配置
    /// </summary>
    public LightningArcConfig? Config
    {
        get => _config;
        set
        {
            _config = value?.Clone();
            if (_renderer != null && _config != null)
            {
                _renderer.UpdateConfig(_config);
                _renderer.SetTargetProgress(_config.Progress);
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 进度（0-100）
    /// </summary>
    public double Progress
    {
        get => _progress;
        set
        {
            _progress = Math.Clamp(value, 0, 100);
            _renderer?.SetTargetProgress((float)(_progress / 100d));
            Invalidate();
        }
    }

    /// <summary>
    /// 当前显示进度（0-100，弹性插值后的值，供外部 UI 显示）
    /// </summary>
    public double DisplayProgress => _renderer?.DisplayProgress * 100d ?? _progress;

    /// <summary>
    /// 是否启用动画
    /// </summary>
    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        set
        {
            _isAnimationEnabled = value;
            if (value)
                StartAnimation();
            else
                StopAnimation();
        }
    }

    /// <summary>
    /// 帧率限制
    /// </summary>
    public int MaxFps
    {
        get => _maxFps;
        set => _maxFps = Math.Max(0, value);
    }

    /// <summary>
    /// 是否允许拖拽调节进度
    /// </summary>
    public bool EnableDragInteraction
    {
        get => _enableDragInteraction;
        set => _enableDragInteraction = value;
    }

    public LightningArcProgressControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        DoubleBuffered = true;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        InitializeRenderer();
        if (_isAnimationEnabled)
            StartAnimation();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        StopAnimation();
        CleanupRenderer();
        base.OnHandleDestroyed(e);
    }

    private void InitializeRenderer()
    {
        var config = _config ?? new LightningArcConfig();
        _renderer = new LightningArcRenderer(config);
        _renderer.SetTargetProgress((float)(_progress / 100d));
        _stopwatch.Start();
    }

    private void CleanupRenderer()
    {
        _renderer?.Dispose();
        _renderer = null;
        _cachedBitmap?.Dispose();
        _cachedBitmap = null;
    }

    private void StartAnimation()
    {
        if (_animationTimer != null || !IsHandleCreated)
            return;

        var interval = _maxFps > 0 ? Math.Max(1, 1000 / _maxFps) : 16;

        _animationTimer = new System.Windows.Forms.Timer
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
        _animationTimer.Dispose();
        _animationTimer = null;
        _stopwatch.Stop();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (_renderer == null)
            return;

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_renderer == null)
        {
            base.OnPaint(e);
            return;
        }

        var width = ClientSize.Width;
        var height = ClientSize.Height;

        if (width <= 0 || height <= 0)
            return;

        var time = _stopwatch.Elapsed.TotalSeconds;

        // 按尺寸复用 SKBitmap
        if (_cachedBitmap == null || _cachedBitmap.Width != width || _cachedBitmap.Height != height)
        {
            _cachedBitmap?.Dispose();
            _cachedBitmap = _renderer.RenderToBitmap(time, width, height);
        }
        else
        {
            _renderer.RenderToBitmap(time, width, height, _cachedBitmap);
        }

        using var bmp = new System.Drawing.Bitmap(
            width,
            height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        CopySKBitmapToGdiBitmap(_cachedBitmap, bmp);

        e.Graphics.DrawImage(bmp, 0, 0);
    }

    private static void CopySKBitmapToGdiBitmap(SKBitmap source, System.Drawing.Bitmap target)
    {
        var targetData = target.LockBits(
            new System.Drawing.Rectangle(0, 0, target.Width, target.Height),
            System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        try
        {
            var sourcePtr = source.GetPixels();
            var targetPtr = targetData.Scan0;
            var sourceStride = source.RowBytes;
            var targetStride = targetData.Stride;

            unsafe
            {
                var src = (byte*)sourcePtr;
                var dst = (byte*)targetPtr;

                for (int y = 0; y < source.Height; y++)
                {
                    for (int x = 0; x < source.Width; x++)
                    {
                        var srcOffset = y * sourceStride + x * 4;
                        var dstOffset = y * targetStride + x * 4;

                        dst[dstOffset + 0] = src[srcOffset + 2]; // B
                        dst[dstOffset + 1] = src[srcOffset + 1]; // G
                        dst[dstOffset + 2] = src[srcOffset + 0]; // R
                        dst[dstOffset + 3] = src[srcOffset + 3]; // A
                    }
                }
            }
        }
        finally
        {
            target.UnlockBits(targetData);
        }
    }

    // ==================== 拖拽交互 ====================

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (_renderer == null || !_enableDragInteraction || e.Button != MouseButtons.Left)
            return;

        _isDragging = true;
        _renderer.SetDragging(true);
        UpdateProgressFromPosition(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isDragging || _renderer == null)
            return;

        UpdateProgressFromPosition(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_isDragging)
            return;

        _isDragging = false;
        _renderer?.SetDragging(false);
    }

    private void UpdateProgressFromPosition(int x)
    {
        var width = ClientSize.Width;
        if (width <= 0)
            return;

        var value = Math.Clamp(x / (double)width * 100d, 0d, 100d);
        Progress = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                StopAnimation();
                CleanupRenderer();
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}
