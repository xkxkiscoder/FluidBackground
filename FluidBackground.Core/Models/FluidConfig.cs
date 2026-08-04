namespace FluidBackground.Core.Models;

/// <summary>
/// 渲染效果模式
/// </summary>
public enum FluidMode
{
    /// <summary>
    /// 流体流动效果
    /// </summary>
    Fluid,

    /// <summary>
    /// 星空效果（闪烁星点、星云、流星）
    /// </summary>
    Starfield,

    /// <summary>
    /// 星云胶囊效果（多层噪声、星点、云团与旋涡构成宇宙星云材质）
    /// </summary>
    Nebula,

    /// <summary>
    /// 极光效果（低频噪声与多条柔光带生成平滑迁移的渐变）
    /// </summary>
    Aurora
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
/// 极光效果配置文件
/// </summary>
public enum AuroraProfile
{
    /// <summary>
    /// POLAR：深色胶囊，橙色、洋红与暖白柔光带
    /// </summary>
    Polar,

    /// <summary>
    /// DUBDOT：白色胶囊，浅蓝、天蓝与青蓝柔光带
    /// </summary>
    Dubdot,

    /// <summary>
    /// VERCEL：白色胶囊，薄荷绿、淡黄与浅粉柔光带
    /// </summary>
    Vercel
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
    /// 图案分布浓度（0.0-1.0），值越小纹理越稀疏、观感越淡雅，1.0 为最浓
    /// </summary>
    public float Density { get; set; } = 0.3f;

    /// <summary>
    /// 效果模式（Fluid / Starfield / Nebula / Aurora）
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
    /// 是否显示流星（星空模式）
    /// </summary>
    public bool EnableMeteor { get; set; } = true;

    /// <summary>
    /// 是否显示星云（星空模式）
    /// </summary>
    public bool EnableNebula { get; set; } = true;

    /// <summary>
    /// 是否启用指针交互
    /// </summary>
    public bool EnablePointerInteraction { get; set; } = true;

    /// <summary>
    /// 指针影响半径（0.0-1.0，相对于画布尺寸）
    /// </summary>
    public float PointerRadius { get; set; } = 0.3f;

    /// <summary>
    /// 随机种子（用于星云和极光效果的形态生成）
    /// </summary>
    public float Seed { get; set; } = 1.7f;

    /// <summary>
    /// 极光效果配置文件（仅在Aurora模式下生效）
    /// </summary>
    public AuroraProfile AuroraProfile { get; set; } = AuroraProfile.Polar;

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
        Density = Density,
        Mode = Mode,
        RenderMode = RenderMode,
        RenderQuality = RenderQuality,
        EnableMeteor = EnableMeteor,
        EnableNebula = EnableNebula,
        EnablePointerInteraction = EnablePointerInteraction,
        PointerRadius = PointerRadius,
        Seed = Seed,
        AuroraProfile = AuroraProfile
    };
}
