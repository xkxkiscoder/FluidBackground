using FluidBackground.Core.Models;
using FluidBackground.Core.Renderers;
using SkiaSharp;

namespace FluidBackground.Core;

/// <summary>
/// 流体背景渲染器主类，提供统一的渲染接口
/// </summary>
public class FluidRenderer : IDisposable
{
    private IFluidRenderer? _renderer;
    private FluidConfig _config;
    private readonly object _lock = new();
    private bool _disposed;

    private FluidRenderer(FluidConfig config)
    {
        _config = config.Clone();
    }

    /// <summary>
    /// 创建流体渲染器实例
    /// </summary>
    /// <param name="config">流体配置</param>
    /// <returns>渲染器实例</returns>
    public static FluidRenderer Create(FluidConfig? config = null)
    {
        config ??= new FluidConfig();
        var instance = new FluidRenderer(config);
        instance.InitializeRenderer();
        return instance;
    }

    /// <summary>
    /// 创建带OpenGL上下文的流体渲染器实例（用于3D渲染）
    /// </summary>
    /// <param name="gl">OpenGL上下文</param>
    /// <param name="config">流体配置</param>
    /// <returns>渲染器实例</returns>
    public static FluidRenderer CreateWithOpenGL(Silk.NET.OpenGL.GL gl, FluidConfig? config = null)
    {
        config ??= new FluidConfig();
        config.RenderMode = RenderMode.Force3D;
        var instance = new FluidRenderer(config);
        instance.InitializeRenderer(gl);
        return instance;
    }

    private void InitializeRenderer(Silk.NET.OpenGL.GL? gl = null)
    {
        switch (_config.RenderMode)
        {
            case RenderMode.Force2D:
                _renderer = new SkiaRenderer();
                break;
            case RenderMode.Force3D:
                _renderer = gl != null ? new OpenGLRenderer(gl) : new OpenGLRenderer();
                break;
            case RenderMode.Auto:
            default:
                _renderer = TryCreateAutoRenderer(gl);
                break;
        }

        if (!_renderer.Initialize(_config))
        {
            if (_config.RenderMode == RenderMode.Auto)
            {
                _renderer.Dispose();
                _renderer = new SkiaRenderer();
                _renderer.Initialize(_config);
            }
        }
    }

    private static IFluidRenderer TryCreateAutoRenderer(Silk.NET.OpenGL.GL? gl)
    {
        if (gl != null)
        {
            var openglRenderer = new OpenGLRenderer(gl);
            return openglRenderer;
        }

        return new SkiaRenderer();
    }

    /// <summary>
    /// 渲染一帧，返回RGBA像素数据
    /// </summary>
    /// <param name="timeSeconds">当前时间（秒）</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>RGBA像素数据</returns>
    public byte[] RenderFrame(double timeSeconds, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (width <= 0 || height <= 0)
            throw new ArgumentException("宽度和高度必须大于0");

        lock (_lock)
        {
            return _renderer!.RenderFrame(timeSeconds, width, height);
        }
    }

    /// <summary>
    /// 渲染到SKBitmap
    /// </summary>
    /// <param name="timeSeconds">当前时间（秒）</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>SKBitmap实例，调用方负责释放</returns>
    public SKBitmap RenderToBitmap(double timeSeconds, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (width <= 0 || height <= 0)
            throw new ArgumentException("宽度和高度必须大于0");

        lock (_lock)
        {
            return _renderer!.RenderToBitmap(timeSeconds, width, height);
        }
    }

    /// <summary>
    /// 渲染到SKCanvas（用于适配层）
    /// </summary>
    public void RenderToCanvas(SKCanvas canvas, double timeSeconds, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_renderer is SkiaRenderer skiaRenderer)
            {
                skiaRenderer.RenderToCanvas(canvas, timeSeconds, width, height);
            }
            else
            {
                using var bitmap = _renderer!.RenderToBitmap(timeSeconds, width, height);
                canvas.DrawBitmap(bitmap, 0, 0);
            }
        }
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    /// <param name="config">新配置</param>
    public void UpdateConfig(FluidConfig config)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(config);

        lock (_lock)
        {
            _config = config.Clone();
            _renderer?.UpdateConfig(_config);
        }
    }

    /// <summary>
    /// 设置指针位置（归一化坐标0-1）
    /// </summary>
    /// <param name="x">X坐标（0-1）</param>
    /// <param name="y">Y坐标（0-1）</param>
    public void SetPointerPosition(float x, float y)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _renderer?.SetPointerPosition(x, y);
        }
    }

    /// <summary>
    /// 获取当前配置的副本
    /// </summary>
    public FluidConfig GetConfig() => _config.Clone();

    /// <summary>
    /// 获取当前渲染器名称
    /// </summary>
    public string RendererName => _renderer?.Name ?? "未初始化";

    /// <summary>
    /// 渲染器是否可用
    /// </summary>
    public bool IsAvailable => _renderer?.IsAvailable ?? false;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                _renderer?.Dispose();
                _renderer = null;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
