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
/// <example>
/// <code>
/// // 基本使用示例
/// var config = new FluidConfig
/// {
///     Colors = FluidPresets.GeneralColors[0].Colors,  // 使用预设颜色
///     Mode = FluidMode.Nebula,                         // 星云模式
///     Speed = 0.5f,                                    // 中等速度
///     Seed = 1.7f                                      // 随机种子
/// };
/// renderer.UpdateConfig(config);
/// </code>
/// </example>
/// </summary>
public class FluidConfig
{
    /// <summary>
    /// 渐变颜色数组（3-6色）
    /// <para>定义动画中使用的渐变颜色，颜色之间会平滑过渡</para>
    /// <para>建议使用4色以获得最佳效果，可通过 <see cref="FluidPresets.GeneralColors"/> 获取预设颜色</para>
    /// <para>创建颜色：<c>FluidColor.FromHex("#FF6B35")</c> 或 <c>new FluidColor(r, g, b)</c>（0-1范围）</para>
    /// </summary>
    public FluidColor[] Colors { get; set; } = DefaultColors;

    /// <summary>
    /// 动画速度（0.1-5.0）
    /// <para>控制动画播放速度，1.0为正常速度</para>
    /// <para>推荐值：流体模式 0.5-1.5，星云模式 0.3-0.6，极光模式 0.3-0.4</para>
    /// </summary>
    public float Speed { get; set; } = 1.0f;

    /// <summary>
    /// 图案分布浓度（0.0-1.0）
    /// <para>值越小纹理越稀疏、观感越淡雅；1.0 为最浓</para>
    /// <para>仅对流体和星空模式生效，星云和极光模式不使用此参数</para>
    /// <para>推荐值：0.2-0.5（淡雅），0.6-1.0（浓郁）</para>
    /// </summary>
    public float Density { get; set; } = 0.3f;

    /// <summary>
    /// 效果模式
    /// <para>决定动画的视觉风格：</para>
    /// <list type="bullet">
    /// <item><see cref="FluidMode.Fluid"/>：流动的渐变动画，适合科技感背景</item>
    /// <item><see cref="FluidMode.Starfield"/>：闪烁星点、星云、流星，适合深空主题</item>
    /// <item><see cref="FluidMode.Nebula"/>：多层噪声、云团与旋涡，来自nebula-capsules项目</item>
    /// <item><see cref="FluidMode.Aurora"/>：柔光带渐变，来自nebula-capsules项目</item>
    /// </list>
    /// </summary>
    public FluidMode Mode { get; set; } = FluidMode.Fluid;

    /// <summary>
    /// 渲染后端模式
    /// <para>决定使用哪个渲染引擎：</para>
    /// <list type="bullet">
    /// <item><see cref="RenderMode.Auto"/>：自动选择，优先3D，失败回退2D（推荐）</item>
    /// <item><see cref="RenderMode.Force2D"/>：强制使用SkiaSharp 2D渲染，兼容性最好</item>
    /// <item><see cref="RenderMode.Force3D"/>：强制使用OpenGL 3D渲染，效果更好但需要OpenGL 3.3+</item>
    /// </list>
    /// </summary>
    public RenderMode RenderMode { get; set; } = RenderMode.Auto;

    /// <summary>
    /// 渲染精度（1.0-4.0）
    /// <para>1.0为最高精度（原始分辨率），4.0为最低精度（1/4分辨率）</para>
    /// <para>降低精度可提高性能，但会牺牲画质</para>
    /// <para>仅对流体模式生效，星空/星云/极光模式始终使用全分辨率</para>
    /// <para>推荐值：1.0（高质量），2.0（平衡），4.0（高性能）</para>
    /// </summary>
    public float RenderQuality { get; set; } = 1.0f;

    /// <summary>
    /// 是否显示流星（仅星空模式生效）
    /// <para>开启后会在星空背景上显示随机出现的流星效果</para>
    /// </summary>
    public bool EnableMeteor { get; set; } = true;

    /// <summary>
    /// 是否显示星云（仅星空模式生效）
    /// <para>开启后会在星空背景上显示彩色星云效果</para>
    /// </summary>
    public bool EnableNebula { get; set; } = true;

    /// <summary>
    /// 是否启用指针交互
    /// <para>开启后鼠标/触摸位置会影响动画效果（如旋涡方向、柔光带弯曲等）</para>
    /// <para>对所有模式都生效</para>
    /// </summary>
    public bool EnablePointerInteraction { get; set; } = true;

    /// <summary>
    /// 指针影响半径（0.0-1.0，相对于画布尺寸）
    /// <para>控制指针交互的影响范围，值越大影响范围越广</para>
    /// <para>推荐值：0.2-0.5</para>
    /// </summary>
    public float PointerRadius { get; set; } = 0.3f;

    /// <summary>
    /// 随机种子（0.0-100.0）
    /// <para>用于星云和极光效果的形态生成，不同种子产生不同视觉效果</para>
    /// <para>可通过 <see cref="FluidPresets.NebulaColors"/> 和 <see cref="FluidPresets.AuroraColors"/> 获取预设种子值</para>
    /// <para>推荐值：1.0-90.0，不同值会产生明显不同的视觉效果</para>
    /// </summary>
    public float Seed { get; set; } = 1.7f;

    /// <summary>
    /// 极光效果配置文件（仅在 <see cref="FluidMode.Aurora"/> 模式下生效）
    /// <para>决定极光效果的视觉风格：</para>
    /// <list type="bullet">
    /// <item><see cref="AuroraProfile.Polar"/>：深色胶囊，橙色、洋红与暖白柔光带</item>
    /// <item><see cref="AuroraProfile.Dubdot"/>：白色胶囊，浅蓝、天蓝与青蓝柔光带</item>
    /// <item><see cref="AuroraProfile.Vercel"/>：白色胶囊，薄荷绿、淡黄与浅粉柔光带</item>
    /// </list>
    /// </summary>
    public AuroraProfile AuroraProfile { get; set; } = AuroraProfile.Polar;

    /// <summary>
    /// 默认颜色方案（深蓝→中蓝→浅蓝→紫）
    /// </summary>
    public static FluidColor[] DefaultColors =>
    [
        new(0.0f, 0.15f, 0.35f),   // 深蓝
        new(0.1f, 0.4f, 0.7f),     // 中蓝
        new(0.3f, 0.6f, 0.9f),     // 浅蓝
        new(0.6f, 0.3f, 0.7f)      // 紫色
    ];

    /// <summary>
    /// 创建配置的副本（深拷贝）
    /// <para>用于在修改配置时避免影响原始对象</para>
    /// </summary>
    /// <returns>配置的深拷贝副本</returns>
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
