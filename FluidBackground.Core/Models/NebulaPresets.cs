namespace FluidBackground.Core.Models;

/// <summary>
/// 星云胶囊预设配置，灵感来自 nebula-capsules 项目
/// </summary>
public static class NebulaPresets
{
    /// <summary>
    /// 预设定义
    /// </summary>
    /// <param name="Id">预设唯一标识符</param>
    /// <param name="Code">预设代码（如 NC-01）</param>
    /// <param name="Name">预设名称</param>
    /// <param name="Group">色系分组（warm/cold）</param>
    /// <param name="Seed">随机种子</param>
    /// <param name="Speed">动画速度</param>
    /// <param name="Colors">颜色数组（4色）</param>
    /// <param name="Mode">效果模式（Nebula/Aurora）</param>
    /// <param name="AuroraProfile">极光配置文件（仅Aurora模式）</param>
    public record Preset(
        string Id,
        string Code,
        string Name,
        string Group,
        float Seed,
        float Speed,
        FluidColor[] Colors,
        FluidMode Mode = FluidMode.Nebula,
        AuroraProfile? AuroraProfile = null
    );

    /// <summary>
    /// 配色方案定义
    /// </summary>
    /// <param name="Id">方案唯一标识符</param>
    /// <param name="Name">方案名称</param>
    /// <param name="Colors">颜色数组</param>
    public record ColorScheme(
        string Id,
        string Name,
        FluidColor[] Colors
    );

    /// <summary>
    /// 所有预设（9组：6组星云胶囊 + 3组极光效果）
    /// </summary>
    public static Preset[] All { get; } =
    [
        new("original", "NC-01", "ORIGINAL", "warm", 1.7f, 0.5f,
            [new(1.0f, 0.95f, 0.92f), new(0.96f, 0.70f, 0.48f), new(0.96f, 0.48f, 0.78f), new(0.66f, 0.47f, 0.91f)]),

        new("ocean", "NC-02", "OCEAN", "cold", 8.2f, 0.48f,
            [new(0.92f, 0.96f, 1.0f), new(0.56f, 0.82f, 1.0f), new(0.23f, 0.53f, 0.96f), new(0.42f, 0.35f, 0.91f)]),

        new("klein", "NC-03", "KLEIN", "cold", 14.1f, 0.49f,
            [new(0.93f, 0.95f, 1.0f), new(0.18f, 0.35f, 0.84f), new(0.11f, 0.13f, 0.25f), new(0.88f, 0.48f, 0.26f)]),

        new("ultraviolet", "NC-04", "ULTRAVIOLET", "cold", 23.4f, 0.47f,
            [new(0.95f, 0.93f, 1.0f), new(0.73f, 0.60f, 0.95f), new(0.56f, 0.45f, 0.86f), new(0.84f, 0.85f, 0.36f)]),

        new("chrome", "NC-05", "CHROME", "cold", 37.8f, 0.42f,
            [new(0.96f, 0.96f, 0.97f), new(0.73f, 0.75f, 0.80f), new(0.50f, 0.53f, 0.58f), new(0.29f, 0.31f, 0.35f)]),

        new("plus", "NC-06", "PLUS", "warm", 51.3f, 0.5f,
            [new(1.0f, 0.94f, 0.90f), new(0.96f, 0.76f, 0.42f), new(0.98f, 0.54f, 0.39f), new(0.91f, 0.43f, 0.45f)]),

        new("polar", "NC-07", "POLAR", "warm", 67.4f, 0.34f,
            [new(0.13f, 0.13f, 0.15f), new(1.0f, 0.48f, 0.10f), new(1.0f, 0.13f, 0.83f), new(1.0f, 0.97f, 0.64f)],
            FluidMode.Aurora, AuroraProfile.Polar),

        new("dubdot", "NC-08", "DUBDOT", "cold", 78.6f, 0.30f,
            [new(1.0f, 1.0f, 1.0f), new(0.87f, 0.93f, 1.0f), new(0.65f, 0.86f, 1.0f), new(0.15f, 0.72f, 0.95f)],
            FluidMode.Aurora, AuroraProfile.Dubdot),

        new("vercel", "NC-09", "VERCEL", "warm", 89.9f, 0.36f,
            [new(1.0f, 1.0f, 1.0f), new(0.74f, 0.94f, 0.92f), new(1.0f, 0.84f, 0.42f), new(1.0f, 0.55f, 0.71f)],
            FluidMode.Aurora, AuroraProfile.Vercel)
    ];

    /// <summary>
    /// 预设配色方案（用于流体和星空模式）
    /// </summary>
    public static ColorScheme[] ColorSchemes { get; } =
    [
        new("deep-sea", "深海蓝紫",
            [new(0.05f, 0.11f, 0.16f), new(0.11f, 0.16f, 0.22f), new(0.18f, 0.25f, 0.34f), new(0.48f, 0.18f, 0.56f)]),

        new("sunset", "日落橙红",
            [new(1.0f, 0.42f, 0.21f), new(0.97f, 0.57f, 0.12f), new(1.0f, 0.27f, 0.27f), new(0.8f, 0.0f, 0.0f)]),

        new("emerald", "翡翠绿",
            [new(0.02f, 0.31f, 0.23f), new(0.02f, 0.47f, 0.34f), new(0.06f, 0.72f, 0.51f), new(0.43f, 0.91f, 0.72f)]),

        new("aurora-blue", "极光蓝",
            [new(0.0f, 0.71f, 0.85f), new(0.0f, 0.47f, 0.71f), new(0.56f, 0.88f, 0.94f), new(0.79f, 0.94f, 0.97f)]),

        new("lavender", "薰衣草",
            [new(0.49f, 0.23f, 0.93f), new(0.65f, 0.55f, 0.98f), new(0.77f, 0.71f, 0.99f), new(0.87f, 0.84f, 0.99f)]),

        new("sakura", "樱花",
            [new(0.93f, 0.23f, 0.49f), new(0.98f, 0.72f, 0.65f), new(0.99f, 0.71f, 0.77f), new(1.0f, 0.84f, 0.93f)])
    ];

    /// <summary>
    /// 根据ID获取预设
    /// </summary>
    /// <param name="id">预设ID</param>
    /// <returns>预设定义，未找到返回null</returns>
    public static Preset? GetById(string id) =>
        All.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// 根据ID获取配色方案
    /// </summary>
    /// <param name="id">配色方案ID</param>
    /// <returns>配色方案定义，未找到返回null</returns>
    public static ColorScheme? GetColorSchemeById(string id) =>
        ColorSchemes.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// 获取预设的FluidConfig
    /// </summary>
    /// <param name="preset">预设定义</param>
    /// <returns>配置实例，可直接用于渲染器</returns>
    public static FluidConfig ToConfig(Preset preset) => new()
    {
        Colors = preset.Colors,
        Speed = preset.Speed,
        Mode = preset.Mode,
        Seed = preset.Seed,
        AuroraProfile = preset.AuroraProfile ?? AuroraProfile.Polar
    };

    /// <summary>
    /// 获取配色方案的FluidConfig
    /// </summary>
    /// <param name="scheme">配色方案</param>
    /// <param name="mode">效果模式</param>
    /// <returns>配置实例，可直接用于渲染器</returns>
    public static FluidConfig ToConfig(ColorScheme scheme, FluidMode mode = FluidMode.Fluid) => new()
    {
        Colors = scheme.Colors,
        Mode = mode
    };

    /// <summary>
    /// 获取所有暖色预设（ORIGINAL、PLUS、POLAR、VERCEL）
    /// </summary>
    public static Preset[] WarmPresets => All.Where(p => p.Group == "warm").ToArray();

    /// <summary>
    /// 获取所有冷色预设（OCEAN、KLEIN、ULTRAVIOLET、CHROME、DUBDOT）
    /// </summary>
    public static Preset[] ColdPresets => All.Where(p => p.Group == "cold").ToArray();

    /// <summary>
    /// 获取所有星云模式预设（NC-01 到 NC-06）
    /// </summary>
    public static Preset[] NebulaPresetsList => All.Where(p => p.Mode == FluidMode.Nebula).ToArray();

    /// <summary>
    /// 获取所有极光模式预设（NC-07 到 NC-09）
    /// </summary>
    public static Preset[] AuroraPresetsList => All.Where(p => p.Mode == FluidMode.Aurora).ToArray();
}
