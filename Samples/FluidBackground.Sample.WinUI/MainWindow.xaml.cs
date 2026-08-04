using FluidBackground.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluidBackground.Sample.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "FluidBackground 控件预览";
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(760, 660));
        this.AppWindow.IsShownInSwitchers = true;
        InitializePreview();
        BindEvents();
    }

    private void InitializePreview()
    {
        // 初始化动画模式列表
        InitializeModeList();

        // 初始化颜色列表
        UpdateColorList(FluidMode.Fluid);

        // 使用默认颜色预设初始化
        var defaultColor = FluidPresets.GeneralColors[0];
        PreviewControl.Config = new FluidConfig
        {
            Colors = defaultColor.Colors,
            Speed = 1.0f,
            Density = 0.3f,
            Mode = FluidMode.Fluid,
            EnablePointerInteraction = true
        };

        // 初始化闪电电弧控件
        ArcControl.Config = new LightningArcConfig
        {
            Theme = LightningArcTheme.BlueWhiteCyan,
            Progress = 0.3f,
            Title = "NEURAL SYNC",
            Subtitle = "SYSTEM ONLINE",
            GlowIntensity = 1.5f,
            JitterAmount = 0.02f,
            ForkChance = 0.3f
        };
    }

    private void InitializeModeList()
    {
        ModeCombo.Items.Clear();
        foreach (var mode in FluidPresets.RenderModes)
        {
            ModeCombo.Items.Add(new ComboBoxItem { Content = mode.Name });
        }
        ModeCombo.SelectedIndex = 0;
    }

    private void BindEvents()
    {
        ModeCombo.SelectionChanged += OnModeChanged;
        SpeedSlider.ValueChanged += OnSpeedChanged;
        DensitySlider.ValueChanged += OnDensityChanged;
        QualitySlider.ValueChanged += OnQualityChanged;
        RenderModeCombo.SelectionChanged += OnRenderModeChanged;
        MeteorCheckBox.Checked += OnMeteorChanged;
        MeteorCheckBox.Unchecked += OnMeteorChanged;
        NebulaCheckBox.Checked += OnNebulaChanged;
        NebulaCheckBox.Unchecked += OnNebulaChanged;

        ThemeCombo.SelectionChanged += OnThemeChanged;
        ProgressSlider.ValueChanged += OnProgressChanged;
    }

    private void OnColorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ColorCombo.SelectedIndex < 0)
            return;

        var mode = GetCurrentMode();
        var colors = FluidPresets.GetColorsForMode(mode);

        if (ColorCombo.SelectedIndex < colors.Length)
        {
            var color = colors[ColorCombo.SelectedIndex];
            UpdateConfig(c => c.Colors = color.Colors);
        }
    }

    private void OnModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModeCombo.SelectedIndex < 0)
            return;

        var mode = GetCurrentMode();
        var isStarfield = mode == FluidMode.Starfield;
        var isNebulaOrAurora = mode == FluidMode.Nebula || mode == FluidMode.Aurora;

        // 星空模式选项
        MeteorCheckBox.Visibility = isStarfield ? Visibility.Visible : Visibility.Collapsed;
        NebulaCheckBox.Visibility = isStarfield ? Visibility.Visible : Visibility.Collapsed;

        // 精度选项（星空和星云/极光模式隐藏）
        QualityLabel.Visibility = !isStarfield && !isNebulaOrAurora ? Visibility.Visible : Visibility.Collapsed;
        QualitySlider.Visibility = !isStarfield && !isNebulaOrAurora ? Visibility.Visible : Visibility.Collapsed;

        // 浓度选项（星云/极光模式隐藏，因为这两种效果不使用浓度）
        DensityLabel.Visibility = !isNebulaOrAurora ? Visibility.Visible : Visibility.Collapsed;
        DensitySlider.Visibility = !isNebulaOrAurora ? Visibility.Visible : Visibility.Collapsed;

        // 更新颜色列表
        UpdateColorList(mode);

        UpdateConfig(c => c.Mode = mode);
    }

    private void UpdateColorList(FluidMode mode)
    {
        var colors = FluidPresets.GetColorsForMode(mode);
        ColorCombo.Items.Clear();
        foreach (var color in colors)
        {
            ColorCombo.Items.Add(new ComboBoxItem { Content = color.Name });
        }
        ColorCombo.SelectedIndex = 0;
    }

    private FluidMode GetCurrentMode()
    {
        if (ModeCombo.SelectedIndex < 0)
            return FluidMode.Fluid;

        return ModeCombo.SelectedIndex switch
        {
            0 => FluidMode.Fluid,
            1 => FluidMode.Starfield,
            2 => FluidMode.Nebula,
            3 => FluidMode.Aurora,
            _ => FluidMode.Fluid
        };
    }

    private void OnSpeedChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        SpeedLabel.Text = $"速度:{e.NewValue:F1}";
        UpdateConfig(c => c.Speed = (float)e.NewValue);
    }

    private void OnDensityChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        DensityLabel.Text = $"浓度:{e.NewValue:F2}";
        UpdateConfig(c => c.Density = (float)e.NewValue);
    }

    private void OnQualityChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        var quality = (float)e.NewValue;
        QualityLabel.Text = $"精度:{quality:F1}x";
        UpdateConfig(c => c.RenderQuality = quality);
    }

    private void OnMeteorChanged(object sender, RoutedEventArgs e)
    {
        UpdateConfig(c => c.EnableMeteor = MeteorCheckBox.IsChecked == true);
    }

    private void OnNebulaChanged(object sender, RoutedEventArgs e)
    {
        UpdateConfig(c => c.EnableNebula = NebulaCheckBox.IsChecked == true);
    }

    private void OnRenderModeChanged(object sender, SelectionChangedEventArgs e)
    {
        var mode = RenderModeCombo.SelectedIndex switch
        {
            0 => RenderMode.Force2D,
            1 => RenderMode.Force3D,
            2 => RenderMode.Auto,
            _ => RenderMode.Auto
        };
        UpdateConfig(c => c.RenderMode = mode);
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ArcControl.Config is not LightningArcConfig config)
            return;

        var theme = ThemeCombo.SelectedIndex switch
        {
            1 => LightningArcTheme.PurpleNavy,
            2 => LightningArcTheme.Clean,
            3 => LightningArcTheme.GreenYellow,
            _ => LightningArcTheme.BlueWhiteCyan
        };

        var newConfig = config.Clone();
        newConfig.Theme = theme;
        ArcControl.Config = newConfig;
    }

    private void OnProgressChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (ProgressLabel != null)
            ProgressLabel.Text = $"{(int)e.NewValue}%";
        if (ArcControl?.Config is LightningArcConfig)
            ArcControl.Progress = e.NewValue;
    }

    private void UpdateConfig(Action<FluidConfig> update)
    {
        if (PreviewControl.Config is FluidConfig config)
        {
            var newConfig = config.Clone();
            update(newConfig);
            PreviewControl.Config = newConfig;
        }
    }
}
