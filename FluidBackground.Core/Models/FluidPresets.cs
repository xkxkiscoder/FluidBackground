namespace FluidBackground.Core.Models;

/// <summary>
/// 流体背景预设配置
/// </summary>
public static class FluidPresets
{
    // ==================== 渲染模式 ====================

    /// <summary>
    /// 渲染模式定义
    /// </summary>
    /// <param name="Mode">效果模式</param>
    /// <param name="Name">模式名称</param>
    /// <param name="Description">模式描述</param>
    /// <param name="DefaultSpeed">默认速度</param>
    /// <param name="DefaultDensity">默认浓度</param>
    /// <param name="AuroraProfile">极光配置（仅Aurora模式）</param>
    public record RenderMode(
        FluidMode Mode,
        string Name,
        string Description,
        float DefaultSpeed = 1.0f,
        float DefaultDensity = 0.3f,
        AuroraProfile? AuroraProfile = null
    );

    /// <summary>
    /// 可用的渲染模式
    /// </summary>
    public static RenderMode[] RenderModes { get; } =
    [
        new(FluidMode.Fluid, "流体", "流动的渐变动画效果", 1.0f, 0.3f),
        new(FluidMode.Starfield, "星空", "闪烁星点、星云、流星效果", 1.0f, 0.3f),
        new(FluidMode.Nebula, "星云", "多层噪声、星点、云团与旋涡构成宇宙星云材质", 0.5f, 0.3f),
        new(FluidMode.Aurora, "极光", "低频噪声与多条柔光带生成平滑迁移的渐变", 0.34f, 0.3f, AuroraProfile.Polar)
    ];

    // ==================== 颜色预设 ====================

    /// <summary>
    /// 颜色预设定义
    /// </summary>
    /// <param name="Id">预设唯一标识符</param>
    /// <param name="Name">预设名称</param>
    /// <param name="Group">色系分组（warm/cold）</param>
    /// <param name="Colors">颜色数组（4色）</param>
    public record ColorPreset(
        string Id,
        string Name,
        string Group,
        FluidColor[] Colors
    );

    /// <summary>
    /// 通用颜色预设（适用于流体和星空模式）
    /// </summary>
    public static ColorPreset[] GeneralColors { get; } =
    [
        new("deep-sea", "深海蓝紫", "cold",
            [new(0.05f, 0.11f, 0.16f), new(0.11f, 0.16f, 0.22f), new(0.18f, 0.25f, 0.34f), new(0.48f, 0.18f, 0.56f)]),

        new("sunset", "日落橙红", "warm",
            [new(1.0f, 0.42f, 0.21f), new(0.97f, 0.57f, 0.12f), new(1.0f, 0.27f, 0.27f), new(0.8f, 0.0f, 0.0f)]),

        new("emerald", "翡翠绿", "cold",
            [new(0.02f, 0.31f, 0.23f), new(0.02f, 0.47f, 0.34f), new(0.06f, 0.72f, 0.51f), new(0.43f, 0.91f, 0.72f)]),

        new("aurora-blue", "极光蓝", "cold",
            [new(0.0f, 0.71f, 0.85f), new(0.0f, 0.47f, 0.71f), new(0.56f, 0.88f, 0.94f), new(0.79f, 0.94f, 0.97f)]),

        new("lavender", "薰衣草", "cold",
            [new(0.49f, 0.23f, 0.93f), new(0.65f, 0.55f, 0.98f), new(0.77f, 0.71f, 0.99f), new(0.87f, 0.84f, 0.99f)]),

        new("sakura", "樱花", "warm",
            [new(0.93f, 0.23f, 0.49f), new(0.98f, 0.72f, 0.65f), new(0.99f, 0.71f, 0.77f), new(1.0f, 0.84f, 0.93f)])
    ];

    /// <summary>
    /// 星云胶囊颜色预设（NC-01 到 NC-06）
    /// </summary>
    public static ColorPreset[] NebulaColors { get; } =
    [
        new("original", "ORIGINAL (NC-01)", "warm",
            [new(1.0f, 0.95f, 0.92f), new(0.96f, 0.70f, 0.48f), new(0.96f, 0.48f, 0.78f), new(0.66f, 0.47f, 0.91f)]),

        new("ocean", "OCEAN (NC-02)", "cold",
            [new(0.92f, 0.96f, 1.0f), new(0.56f, 0.82f, 1.0f), new(0.23f, 0.53f, 0.96f), new(0.42f, 0.35f, 0.91f)]),

        new("klein", "KLEIN (NC-03)", "cold",
            [new(0.93f, 0.95f, 1.0f), new(0.18f, 0.35f, 0.84f), new(0.11f, 0.13f, 0.25f), new(0.88f, 0.48f, 0.26f)]),

        new("ultraviolet", "ULTRAVIOLET (NC-04)", "cold",
            [new(0.95f, 0.93f, 1.0f), new(0.73f, 0.60f, 0.95f), new(0.56f, 0.45f, 0.86f), new(0.84f, 0.85f, 0.36f)]),

        new("chrome", "CHROME (NC-05)", "cold",
            [new(0.96f, 0.96f, 0.97f), new(0.73f, 0.75f, 0.80f), new(0.50f, 0.53f, 0.58f), new(0.29f, 0.31f, 0.35f)]),

        new("plus", "PLUS (NC-06)", "warm",
            [new(1.0f, 0.94f, 0.90f), new(0.96f, 0.76f, 0.42f), new(0.98f, 0.54f, 0.39f), new(0.91f, 0.43f, 0.45f)])
    ];

    /// <summary>
    /// 极光颜色预设（NC-07 到 NC-09）
    /// </summary>
    public static ColorPreset[] AuroraColors { get; } =
    [
        new("polar", "POLAR (NC-07)", "warm",
            [new(0.13f, 0.13f, 0.15f), new(1.0f, 0.48f, 0.10f), new(1.0f, 0.13f, 0.83f), new(1.0f, 0.97f, 0.64f)]),

        new("dubdot", "DUBDOT (NC-08)", "cold",
            [new(1.0f, 1.0f, 1.0f), new(0.87f, 0.93f, 1.0f), new(0.65f, 0.86f, 1.0f), new(0.15f, 0.72f, 0.95f)]),

        new("vercel", "VERCEL (NC-09)", "warm",
            [new(1.0f, 1.0f, 1.0f), new(0.74f, 0.94f, 0.92f), new(1.0f, 0.84f, 0.42f), new(1.0f, 0.55f, 0.71f)])
    ];

    // ==================== 便捷方法 ====================

    /// <summary>
    /// 根据模式获取对应的渲染模式定义
    /// </summary>
    public static RenderMode? GetRenderMode(FluidMode mode) =>
        RenderModes.FirstOrDefault(r => r.Mode == mode);

    /// <summary>
    /// 根据ID获取颜色预设（搜索所有颜色预设）
    /// </summary>
    public static ColorPreset? GetColorById(string id) =>
        GeneralColors.FirstOrDefault(c => c.Id == id) ??
        NebulaColors.FirstOrDefault(c => c.Id == id) ??
        AuroraColors.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// 获取指定模式的颜色预设列表
    /// </summary>
    public static ColorPreset[] GetColorsForMode(FluidMode mode) => mode switch
    {
        FluidMode.Nebula => NebulaColors,
        FluidMode.Aurora => AuroraColors,
        _ => GeneralColors
    };

    /// <summary>
    /// 创建FluidConfig
    /// </summary>
    /// <param name="mode">渲染模式</param>
    /// <param name="color">颜色预设</param>
    /// <returns>配置实例</returns>
    public static FluidConfig CreateConfig(RenderMode mode, ColorPreset color) => new()
    {
        Colors = color.Colors,
        Mode = mode.Mode,
        Speed = mode.DefaultSpeed,
        Density = mode.DefaultDensity,
        AuroraProfile = mode.AuroraProfile ?? AuroraProfile.Polar
    };

    /// <summary>
    /// 创建FluidConfig
    /// </summary>
    /// <param name="mode">效果模式枚举</param>
    /// <param name="colorId">颜色预设ID</param>
    /// <returns>配置实例，如果颜色ID无效返回null</returns>
    public static FluidConfig? CreateConfig(FluidMode mode, string colorId)
    {
        var renderMode = GetRenderMode(mode);
        var color = GetColorById(colorId);
        if (renderMode == null || color == null)
            return null;
        return CreateConfig(renderMode, color);
    }
}
