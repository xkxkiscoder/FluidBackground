namespace FluidBackground.Core.Models;

/// <summary>
/// 通用颜色预设枚举（适用于流体和星空模式）
/// <example>
/// <code>
/// renderer.UpdateConfig(FluidPresets.CreateConfig(GeneralColorPreset.DeepSea));
/// </code>
/// </example>
/// </summary>
public enum GeneralColorPreset
{
    /// <summary>深海蓝紫 - 深邃的海洋蓝紫色调，适合科技、神秘主题</summary>
    DeepSea,
    /// <summary>日落橙红 - 温暖的日落橙红色调，充满活力与激情</summary>
    Sunset,
    /// <summary>翡翠绿 - 清新的翡翠绿色调，自然、生机盎然</summary>
    Emerald,
    /// <summary>极光蓝 - 极地冰川般的蓝色调，清冷而纯净</summary>
    AuroraBlue,
    /// <summary>薰衣草 - 优雅的薰衣草紫色调，浪漫而梦幻</summary>
    Lavender,
    /// <summary>樱花 - 柔和的樱花粉色调，温柔而甜美</summary>
    Sakura
}

/// <summary>
/// 星云胶囊颜色预设枚举（适用于星云模式）
/// <example>
/// <code>
/// renderer.UpdateConfig(FluidPresets.CreateConfig(NebulaColorPreset.Ocean));
/// </code>
/// </example>
/// </summary>
public enum NebulaColorPreset
{
    /// <summary>ORIGINAL (NC-01) - 奶油白、橙色、粉色、紫色的温暖组合</summary>
    Original,
    /// <summary>OCEAN (NC-02) - 浅蓝、天蓝、蓝色、靛蓝的海洋色调</summary>
    Ocean,
    /// <summary>KLEIN (NC-03) - 浅蓝、深蓝、深紫、橙色的克莱因蓝风格</summary>
    Klein,
    /// <summary>ULTRAVIOLET (NC-04) - 浅紫、紫色、深紫、黄绿的紫外线风格</summary>
    Ultraviolet,
    /// <summary>CHROME (NC-05) - 浅灰、银灰、灰、深灰的铬金属风格</summary>
    Chrome,
    /// <summary>PLUS (NC-06) - 奶油白、金色、橙色、红色的PLUS风格</summary>
    Plus
}

/// <summary>
/// 极光颜色预设枚举（适用于极光模式）
/// <example>
/// <code>
/// renderer.UpdateConfig(FluidPresets.CreateConfig(AuroraColorPreset.Polar));
/// </code>
/// </example>
/// </summary>
public enum AuroraColorPreset
{
    /// <summary>POLAR (NC-07) - 深色背景、橙色、洋红、暖白的极地风格</summary>
    Polar,
    /// <summary>DUBDOT (NC-08) - 白色背景、浅蓝、天蓝、青蓝的双点风格</summary>
    Dubdot,
    /// <summary>VERCEL (NC-09) - 白色背景、薄荷绿、淡黄、浅粉的Vercel风格</summary>
    Vercel
}

/// <summary>
/// 流体背景预设配置
/// <para>提供两种创建配置的方式：</para>
/// <list type="number">
/// <item>使用预设枚举：<c>FluidPresets.CreateConfig(GeneralColorPreset.DeepSea)</c></item>
/// <item>手动创建：<c>new FluidConfig { ... }</c></item>
/// </list>
/// </summary>
public static class FluidPresets
{
    // ==================== 渲染模式定义 ====================

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
        new(FluidMode.Nebula, "星云", "多层噪声、云团与旋涡构成宇宙星云材质", 0.5f, 0.3f),
        new(FluidMode.Aurora, "极光", "低频噪声与柔光带生成平滑迁移的渐变", 0.34f, 0.3f, AuroraProfile.Polar)
    ];

    // ==================== 颜色预设数据 ====================

    /// <summary>
    /// 颜色预设定义
    /// </summary>
    public record ColorPreset(
        string Id,
        string Name,
        string Code,
        string Group,
        string Description,
        FluidColor[] Colors,
        string[] HexColors
    );

    /// <summary>
    /// 通用颜色预设
    /// </summary>
    internal static ColorPreset[] GeneralColors { get; } =
    [
        new("deep-sea", "深海蓝紫", "GEN-01", "cold", "深邃的海洋蓝紫色调",
            [new(0.05f, 0.11f, 0.16f), new(0.11f, 0.16f, 0.22f), new(0.18f, 0.25f, 0.34f), new(0.48f, 0.18f, 0.56f)],
            ["#0D1B2A", "#1B2838", "#2E4057", "#7B2D8E"]),
        new("sunset", "日落橙红", "GEN-02", "warm", "温暖的日落橙红色调",
            [new(1.0f, 0.42f, 0.21f), new(0.97f, 0.57f, 0.12f), new(1.0f, 0.27f, 0.27f), new(0.8f, 0.0f, 0.0f)],
            ["#FF6B35", "#F7931E", "#FF4444", "#CC0000"]),
        new("emerald", "翡翠绿", "GEN-03", "cold", "清新的翡翠绿色调",
            [new(0.02f, 0.31f, 0.23f), new(0.02f, 0.47f, 0.34f), new(0.06f, 0.72f, 0.51f), new(0.43f, 0.91f, 0.72f)],
            ["#064E3B", "#047857", "#10B981", "#6EE7B7"]),
        new("aurora-blue", "极光蓝", "GEN-04", "cold", "极地冰川般的蓝色调",
            [new(0.0f, 0.71f, 0.85f), new(0.0f, 0.47f, 0.71f), new(0.56f, 0.88f, 0.94f), new(0.79f, 0.94f, 0.97f)],
            ["#00B4D8", "#0077B6", "#90E0EF", "#CAF0F8"]),
        new("lavender", "薰衣草", "GEN-05", "cold", "优雅的薰衣草紫色调",
            [new(0.49f, 0.23f, 0.93f), new(0.65f, 0.55f, 0.98f), new(0.77f, 0.71f, 0.99f), new(0.87f, 0.84f, 0.99f)],
            ["#7C3AED", "#A78BFA", "#C4B5FD", "#DDD6FE"]),
        new("sakura", "樱花", "GEN-06", "warm", "柔和的樱花粉色调",
            [new(0.93f, 0.23f, 0.49f), new(0.98f, 0.72f, 0.65f), new(0.99f, 0.71f, 0.77f), new(1.0f, 0.84f, 0.93f)],
            ["#ED3A7C", "#FAB7A7", "#FDB5C4", "#FED6ED"])
    ];

    /// <summary>
    /// 星云颜色预设
    /// </summary>
    internal static ColorPreset[] NebulaColors { get; } =
    [
        new("original", "ORIGINAL", "NC-01", "warm", "奶油白、橙色、粉色、紫色的温暖组合",
            [new(1.0f, 0.95f, 0.92f), new(0.96f, 0.70f, 0.48f), new(0.96f, 0.48f, 0.78f), new(0.66f, 0.47f, 0.91f)],
            ["#FFF3EA", "#F5B27A", "#F67BC6", "#A978E8"]),
        new("ocean", "OCEAN", "NC-02", "cold", "浅蓝、天蓝、蓝色、靛蓝的海洋色调",
            [new(0.92f, 0.96f, 1.0f), new(0.56f, 0.82f, 1.0f), new(0.23f, 0.53f, 0.96f), new(0.42f, 0.35f, 0.91f)],
            ["#EAF6FF", "#8FD0FF", "#3B87F6", "#6B58E9"]),
        new("klein", "KLEIN", "NC-03", "cold", "浅蓝、深蓝、深紫、橙色的克莱因蓝风格",
            [new(0.93f, 0.95f, 1.0f), new(0.18f, 0.35f, 0.84f), new(0.11f, 0.13f, 0.25f), new(0.88f, 0.48f, 0.26f)],
            ["#EDF2FF", "#2F58D5", "#1B2040", "#E07A43"]),
        new("ultraviolet", "ULTRAVIOLET", "NC-04", "cold", "浅紫、紫色、深紫、黄绿的紫外线风格",
            [new(0.95f, 0.93f, 1.0f), new(0.73f, 0.60f, 0.95f), new(0.56f, 0.45f, 0.86f), new(0.84f, 0.85f, 0.36f)],
            ["#F2EEFF", "#B99AF1", "#8F74DB", "#D7D85C"]),
        new("chrome", "CHROME", "NC-05", "cold", "浅灰、银灰、灰、深灰的铬金属风格",
            [new(0.96f, 0.96f, 0.97f), new(0.73f, 0.75f, 0.80f), new(0.50f, 0.53f, 0.58f), new(0.29f, 0.31f, 0.35f)],
            ["#F5F6F8", "#B9C0CC", "#7F8793", "#4A4F59"]),
        new("plus", "PLUS", "NC-06", "warm", "奶油白、金色、橙色、红色的PLUS风格",
            [new(1.0f, 0.94f, 0.90f), new(0.96f, 0.76f, 0.42f), new(0.98f, 0.54f, 0.39f), new(0.91f, 0.43f, 0.45f)],
            ["#FFF0E6", "#F6C26B", "#F98A64", "#E86D74"])
    ];

    /// <summary>
    /// 极光颜色预设
    /// </summary>
    internal static ColorPreset[] AuroraColors { get; } =
    [
        new("polar", "POLAR", "NC-07", "warm", "深色背景、橙色、洋红、暖白的极地风格",
            [new(0.13f, 0.13f, 0.15f), new(1.0f, 0.48f, 0.10f), new(1.0f, 0.13f, 0.83f), new(1.0f, 0.97f, 0.64f)],
            ["#202126", "#FF7A1A", "#FF22D3", "#FFF7A3"]),
        new("dubdot", "DUBDOT", "NC-08", "cold", "白色背景、浅蓝、天蓝、青蓝的双点风格",
            [new(1.0f, 1.0f, 1.0f), new(0.87f, 0.93f, 1.0f), new(0.65f, 0.86f, 1.0f), new(0.15f, 0.72f, 0.95f)],
            ["#FFFFFF", "#DDEEFF", "#A7DBFF", "#27B8F3"]),
        new("vercel", "VERCEL", "NC-09", "warm", "白色背景、薄荷绿、淡黄、浅粉的Vercel风格",
            [new(1.0f, 1.0f, 1.0f), new(0.74f, 0.94f, 0.92f), new(1.0f, 0.84f, 0.42f), new(1.0f, 0.55f, 0.71f)],
            ["#FFFFFF", "#BCEFEA", "#FFD76A", "#FF8BB6"])
    ];

    // ==================== 枚举映射 ====================

    private static readonly Dictionary<GeneralColorPreset, int> GeneralPresetIndex = new()
    {
        { GeneralColorPreset.DeepSea, 0 },
        { GeneralColorPreset.Sunset, 1 },
        { GeneralColorPreset.Emerald, 2 },
        { GeneralColorPreset.AuroraBlue, 3 },
        { GeneralColorPreset.Lavender, 4 },
        { GeneralColorPreset.Sakura, 5 }
    };

    private static readonly Dictionary<NebulaColorPreset, int> NebulaPresetIndex = new()
    {
        { NebulaColorPreset.Original, 0 },
        { NebulaColorPreset.Ocean, 1 },
        { NebulaColorPreset.Klein, 2 },
        { NebulaColorPreset.Ultraviolet, 3 },
        { NebulaColorPreset.Chrome, 4 },
        { NebulaColorPreset.Plus, 5 }
    };

    private static readonly Dictionary<AuroraColorPreset, int> AuroraPresetIndex = new()
    {
        { AuroraColorPreset.Polar, 0 },
        { AuroraColorPreset.Dubdot, 1 },
        { AuroraColorPreset.Vercel, 2 }
    };

    // ==================== 公共方法 ====================

    /// <summary>
    /// 根据通用颜色枚举创建配置（自动使用流体模式）
    /// </summary>
    /// <param name="preset">通用颜色预设枚举</param>
    /// <returns>配置实例</returns>
    public static FluidConfig CreateConfig(GeneralColorPreset preset)
    {
        var index = GeneralPresetIndex[preset];
        var color = GeneralColors[index];
        return new FluidConfig
        {
            Colors = color.Colors,
            Mode = FluidMode.Fluid,
            Speed = 1.0f,
            Density = 0.3f
        };
    }

    /// <summary>
    /// 根据星云颜色枚举创建配置（自动使用星云模式）
    /// </summary>
    /// <param name="preset">星云颜色预设枚举</param>
    /// <returns>配置实例</returns>
    public static FluidConfig CreateConfig(NebulaColorPreset preset)
    {
        var index = NebulaPresetIndex[preset];
        var color = NebulaColors[index];
        return new FluidConfig
        {
            Colors = color.Colors,
            Mode = FluidMode.Nebula,
            Speed = 0.5f,
            Seed = color.Id switch
            {
                "original" => 1.7f,
                "ocean" => 8.2f,
                "klein" => 14.1f,
                "ultraviolet" => 23.4f,
                "chrome" => 37.8f,
                "plus" => 51.3f,
                _ => 1.7f
            }
        };
    }

    /// <summary>
    /// 根据极光颜色枚举创建配置（自动使用极光模式）
    /// </summary>
    /// <param name="preset">极光颜色预设枚举</param>
    /// <returns>配置实例</returns>
    public static FluidConfig CreateConfig(AuroraColorPreset preset)
    {
        var index = AuroraPresetIndex[preset];
        var color = AuroraColors[index];
        return new FluidConfig
        {
            Colors = color.Colors,
            Mode = FluidMode.Aurora,
            Speed = 0.34f,
            Seed = color.Id switch
            {
                "polar" => 67.4f,
                "dubdot" => 78.6f,
                "vercel" => 89.9f,
                _ => 67.4f
            },
            AuroraProfile = color.Id switch
            {
                "polar" => AuroraProfile.Polar,
                "dubdot" => AuroraProfile.Dubdot,
                "vercel" => AuroraProfile.Vercel,
                _ => AuroraProfile.Polar
            }
        };
    }

    /// <summary>
    /// 获取渲染模式定义
    /// </summary>
    /// <param name="mode">效果模式</param>
    /// <returns>渲染模式定义</returns>
    public static RenderMode? GetRenderMode(FluidMode mode) =>
        RenderModes.FirstOrDefault(r => r.Mode == mode);

    /// <summary>
    /// 获取指定模式的颜色名称列表（用于UI下拉框）
    /// </summary>
    /// <param name="mode">效果模式</param>
    /// <returns>颜色名称数组</returns>
    public static string[] GetColorNames(FluidMode mode) => mode switch
    {
        FluidMode.Nebula => NebulaColors.Select(c => c.Name).ToArray(),
        FluidMode.Aurora => AuroraColors.Select(c => c.Name).ToArray(),
        _ => GeneralColors.Select(c => c.Name).ToArray()
    };

    /// <summary>
    /// 获取指定模式的颜色配置数组
    /// </summary>
    /// <param name="mode">效果模式</param>
    /// <param name="index">颜色索引</param>
    /// <returns>颜色数组，如果索引无效返回默认颜色</returns>
    public static FluidColor[] GetColors(FluidMode mode, int index = 0)
    {
        var colors = mode switch
        {
            FluidMode.Nebula => NebulaColors,
            FluidMode.Aurora => AuroraColors,
            _ => GeneralColors
        };
        
        if (index >= 0 && index < colors.Length)
            return colors[index].Colors;
        
        return colors[0].Colors;
    }
}
