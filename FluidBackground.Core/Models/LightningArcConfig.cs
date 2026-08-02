namespace FluidBackground.Core.Models;

/// <summary>
/// 闪电电弧进度边界配置
/// </summary>
public class LightningArcConfig
{
    /// <summary>
    /// 内置配色主题
    /// </summary>
    public LightningArcTheme Theme { get; set; } = LightningArcTheme.BlueWhiteCyan;

    /// <summary>
    /// 进度值（0.0-1.0），0=未激活，1=充能完成
    /// </summary>
    public float Progress { get; set; } = 0.3f;

    /// <summary>
    /// 自定义发光色（覆盖主题），null 时使用主题色
    /// </summary>
    public FluidColor? GlowColor { get; set; }

    /// <summary>
    /// 自定义卡片背景色（覆盖主题），null 时使用主题色
    /// </summary>
    public FluidColor? BackgroundColor { get; set; }

    /// <summary>
    /// 自定义左侧环境光色（覆盖主题），null 时使用主题色
    /// </summary>
    public FluidColor? AmbientColor { get; set; }

    /// <summary>
    /// 发光强度（0.0-3.0），默认 1.5
    /// </summary>
    public float GlowIntensity { get; set; } = 1.5f;

    /// <summary>
    /// 电弧自然抖动幅度（0.0-0.1，相对于卡片宽度），默认 0.02
    /// </summary>
    public float JitterAmount { get; set; } = 0.02f;

    /// <summary>
    /// 分叉概率（0.0-1.0），默认 0.3
    /// </summary>
    public float ForkChance { get; set; } = 0.3f;

    /// <summary>
    /// 左侧环境光亮度比例（0.0-1.0），默认 0.12
    /// </summary>
    public float AmbientGlow { get; set; } = 0.12f;

    /// <summary>
    /// 标题文字
    /// </summary>
    public string Title { get; set; } = "NEURAL SYNC";

    /// <summary>
    /// 副标题文字（null 或空则不显示）
    /// </summary>
    public string? Subtitle { get; set; } = "SYSTEM ONLINE";

    /// <summary>
    /// 卡片圆角半径（像素，相对于卡片高度，按比例缩放），默认 20
    /// </summary>
    public float CornerRadius { get; set; } = 20f;

    /// <summary>
    /// 主题/颜色切换的插值时长（秒），默认 0.3
    /// </summary>
    public float ColorTransitionSeconds { get; set; } = 0.3f;

    /// <summary>
    /// 是否显示标题与副标题
    /// </summary>
    public bool ShowTitle { get; set; } = true;

    /// <summary>
    /// 是否显示右侧百分比数字
    /// </summary>
    public bool ShowPercentage { get; set; } = true;

    /// <summary>
    /// 是否允许拖拽调节进度（平台控件读取）
    /// </summary>
    public bool EnableDragInteraction { get; set; } = true;

    /// <summary>
    /// 获取主题对应的颜色三元组（发光色、背景色、环境光色）
    /// </summary>
    public static (FluidColor Glow, FluidColor Background, FluidColor Ambient) GetThemeColors(LightningArcTheme theme) =>
        theme switch
        {
            LightningArcTheme.PurpleNavy => (
                FluidColor.FromHex("#B026FF"),
                FluidColor.FromHex("#1A0B2E"),
                FluidColor.FromHex("#2D1B4E")),
            LightningArcTheme.Clean => (
                FluidColor.FromHex("#FF4D00"),
                FluidColor.FromHex("#1A0A05"),
                FluidColor.FromHex("#331010")),
            LightningArcTheme.GreenYellow => (
                FluidColor.FromHex("#CCFF00"),
                FluidColor.FromHex("#0F1400"),
                FluidColor.FromHex("#1A2600")),
            _ => (
                FluidColor.FromHex("#00F5FF"),
                FluidColor.FromHex("#0A1929"),
                FluidColor.FromHex("#001A33"))
        };

    /// <summary>
    /// 当前生效的发光色
    /// </summary>
    public FluidColor EffectiveGlowColor => GlowColor ?? GetThemeColors(Theme).Glow;

    /// <summary>
    /// 当前生效的背景色
    /// </summary>
    public FluidColor EffectiveBackgroundColor => BackgroundColor ?? GetThemeColors(Theme).Background;

    /// <summary>
    /// 当前生效的环境光色
    /// </summary>
    public FluidColor EffectiveAmbientColor => AmbientColor ?? GetThemeColors(Theme).Ambient;

    /// <summary>
    /// 创建配置副本
    /// </summary>
    public LightningArcConfig Clone() => new()
    {
        Theme = Theme,
        Progress = Progress,
        GlowColor = GlowColor,
        BackgroundColor = BackgroundColor,
        AmbientColor = AmbientColor,
        GlowIntensity = GlowIntensity,
        JitterAmount = JitterAmount,
        ForkChance = ForkChance,
        AmbientGlow = AmbientGlow,
        Title = Title,
        Subtitle = Subtitle,
        CornerRadius = CornerRadius,
        ColorTransitionSeconds = ColorTransitionSeconds,
        ShowTitle = ShowTitle,
        ShowPercentage = ShowPercentage,
        EnableDragInteraction = EnableDragInteraction
    };
}
