using FluidBackground.Core.Models;
using SkiaSharp;

namespace FluidBackground.Core.Renderers;

/// <summary>
/// 流体渲染器接口
/// </summary>
public interface IFluidRenderer : IDisposable
{
    /// <summary>
    /// 渲染器名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 是否可用（依赖是否满足）
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 初始化渲染器
    /// </summary>
    /// <param name="config">流体配置</param>
    /// <returns>是否初始化成功</returns>
    bool Initialize(FluidConfig config);

    /// <summary>
    /// 渲染一帧到字节数组（RGBA格式）
    /// </summary>
    /// <param name="timeSeconds">当前时间（秒）</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>RGBA像素数据</returns>
    byte[] RenderFrame(double timeSeconds, int width, int height);

    /// <summary>
    /// 渲染到SKBitmap
    /// </summary>
    /// <param name="timeSeconds">当前时间（秒）</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>SKBitmap实例，调用方负责释放</returns>
    SKBitmap RenderToBitmap(double timeSeconds, int width, int height);

    /// <summary>
    /// 渲染到现有的SKBitmap
    /// </summary>
    /// <param name="timeSeconds">当前时间（秒）</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="target">目标SKBitmap</param>
    void RenderToBitmap(double timeSeconds, int width, int height, SKBitmap target);

    /// <summary>
    /// 更新配置
    /// </summary>
    /// <param name="config">新配置</param>
    void UpdateConfig(FluidConfig config);

    /// <summary>
    /// 设置指针位置（归一化坐标0-1）
    /// </summary>
    /// <param name="x">X坐标（0-1）</param>
    /// <param name="y">Y坐标（0-1）</param>
    void SetPointerPosition(float x, float y);
}
