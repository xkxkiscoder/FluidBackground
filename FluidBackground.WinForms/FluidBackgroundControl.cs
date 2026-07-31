using System.Diagnostics;
using FluidBackground.Core;
using FluidBackground.Core.Models;
using SkiaSharp;

namespace FluidBackground.WinForms;

/// <summary>
/// WinForms平台的流体背景控件
/// </summary>
public class FluidBackgroundControl : Control
{
    private FluidRenderer? _renderer;
    private readonly Stopwatch _stopwatch = new();
    private System.Windows.Forms.Timer? _animationTimer;
    private SKBitmap? _cachedBitmap;
    private bool _disposed;

    private FluidConfig? _config;
    private bool _isAnimationEnabled = true;
    private int _maxFps = 60;

    /// <summary>
    /// 流体配置
    /// </summary>
    public FluidConfig? Config
    {
        get => _config;
        set
        {
            _config = value?.Clone();
            if (_renderer != null && _config != null)
            {
                _renderer.UpdateConfig(_config);
                Invalidate();
            }
        }
    }

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

    public FluidBackgroundControl()
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
        var config = _config ?? new FluidConfig();
        _renderer = FluidRenderer.Create(config);
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
        if (_renderer == null || !_renderer.IsAvailable)
            return;

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_renderer == null || !_renderer.IsAvailable)
        {
            base.OnPaint(e);
            return;
        }

        var width = ClientSize.Width;
        var height = ClientSize.Height;

        if (width <= 0 || height <= 0)
            return;

        var time = _stopwatch.Elapsed.TotalSeconds;

        _cachedBitmap?.Dispose();
        _cachedBitmap = _renderer.RenderToBitmap(time, width, height);

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

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_renderer == null || Config?.EnablePointerInteraction != true)
            return;

        var width = ClientSize.Width;
        var height = ClientSize.Height;

        if (width > 0 && height > 0)
        {
            var x = (float)e.X / width;
            var y = (float)e.Y / height;
            _renderer.SetPointerPosition(x, y);
        }
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
