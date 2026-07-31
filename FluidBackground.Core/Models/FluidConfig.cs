namespace FluidBackground.Core.Models;

/// <summary>
/// 流体动画模式
/// </summary>
public enum FluidMode
{
    /// <summary>
    /// 流体流动效果
    /// </summary>
    Fluid,

    /// <summary>
    /// 波纹扩散效果
    /// </summary>
    Ripple,

    /// <summary>
    /// 呼吸脉动效果
    /// </summary>
    Breathing
}

/// <summary>
/// 渲染模式
/// </summary>
public enum RenderMode
{
    /// <summary>
    /// 自动选择最佳渲染后端
    /// </summary>
    Auto,

    /// <summary>
    /// 强制使用2D渲染（SkiaSharp）
    /// </summary>
    Force2D,

    /// <summary>
    /// 强制使用3D渲染（OpenGL）
    /// </summary>
    Force3D
}

/// <summary>
/// 流体背景配置
/// </summary>
public class FluidConfig
{
    /// <summary>
    /// 渐变颜色数组（3-6色）
    /// </summary>
    public FluidColor[] Colors { get; set; } = DefaultColors;

    /// <summary>
    /// 动画速度（0.1-5.0）
    /// </summary>
    public float Speed { get; set; } = 1.0f;

    /// <summary>
    /// 效果强度（0.0-1.0）
    /// </summary>
    public float Intensity { get; set; } = 0.7f;

    /// <summary>
    /// 流体动画模式
    /// </summary>
    public FluidMode Mode { get; set; } = FluidMode.Fluid;

    /// <summary>
    /// 渲染模式
    /// </summary>
    public RenderMode RenderMode { get; set; } = RenderMode.Auto;

    /// <summary>
    /// 渲染精度（1.0=最高精度，4.0=最低精度，支持无极调节）
    /// </summary>
    public float RenderQuality { get; set; } = 1.0f;

    /// <summary>
    /// 是否启用指针交互
    /// </summary>
    public bool EnablePointerInteraction { get; set; } = true;

    /// <summary>
    /// 指针影响半径（0.0-1.0，相对于画布尺寸）
    /// </summary>
    public float PointerRadius { get; set; } = 0.3f;

    /// <summary>
    /// 默认颜色方案
    /// </summary>
    public static FluidColor[] DefaultColors =>
    [
        new(0.0f, 0.15f, 0.35f),   // 深蓝
        new(0.1f, 0.4f, 0.7f),     // 中蓝
        new(0.3f, 0.6f, 0.9f),     // 浅蓝
        new(0.6f, 0.3f, 0.7f)      // 紫色
    ];

    /// <summary>
    /// 创建配置的副本
    /// </summary>
    public FluidConfig Clone() => new()
    {
        Colors = Colors.Select(c => c).ToArray(),
        Speed = Speed,
        Intensity = Intensity,
        Mode = Mode,
        RenderMode = RenderMode,
        RenderQuality = RenderQuality,
        EnablePointerInteraction = EnablePointerInteraction,
        PointerRadius = PointerRadius
    };
}
