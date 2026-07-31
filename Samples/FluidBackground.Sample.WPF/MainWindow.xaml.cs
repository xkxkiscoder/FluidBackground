using System.Windows;
using System.Windows.Controls;
using FluidBackground.Core.Models;

namespace FluidBackground.Sample.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializePreview();
        BindEvents();
    }

    private void InitializePreview()
    {
        PreviewControl.Config = new FluidConfig
        {
            Colors =
            [
                FluidColor.FromHex("#0D1B2A"),
                FluidColor.FromHex("#1B2838"),
                FluidColor.FromHex("#2E4057"),
                FluidColor.FromHex("#7B2D8E")
            ],
            Speed = 1.0f,
            Density = 0.3f,
            Mode = FluidMode.Fluid,
            EnablePointerInteraction = true
        };
    }

    private void BindEvents()
    {
        ColorCombo.SelectionChanged += OnColorChanged;
        ModeCombo.SelectionChanged += OnModeChanged;
        SpeedSlider.ValueChanged += OnSpeedChanged;
        DensitySlider.ValueChanged += OnDensityChanged;
        QualitySlider.ValueChanged += OnQualityChanged;
        RenderModeCombo.SelectionChanged += OnRenderModeChanged;
        MeteorCheckBox.Checked += OnMeteorChanged;
        MeteorCheckBox.Unchecked += OnMeteorChanged;
        NebulaCheckBox.Checked += OnNebulaChanged;
        NebulaCheckBox.Unchecked += OnNebulaChanged;
    }

    private void OnColorChanged(object sender, SelectionChangedEventArgs e)
    {
        var colors = ColorCombo.SelectedIndex switch
        {
            0 => // 深海蓝紫
            [
                FluidColor.FromHex("#0D1B2A"),
                FluidColor.FromHex("#1B2838"),
                FluidColor.FromHex("#2E4057"),
                FluidColor.FromHex("#7B2D8E")
            ],
            1 => // 日落橙红
            [
                FluidColor.FromHex("#FF6B35"),
                FluidColor.FromHex("#F7931E"),
                FluidColor.FromHex("#FF4444"),
                FluidColor.FromHex("#CC0000")
            ],
            2 => // 翡翠绿
            [
                FluidColor.FromHex("#064E3B"),
                FluidColor.FromHex("#047857"),
                FluidColor.FromHex("#10B981"),
                FluidColor.FromHex("#6EE7B7")
            ],
            3 => // 极光
            [
                FluidColor.FromHex("#00B4D8"),
                FluidColor.FromHex("#0077B6"),
                FluidColor.FromHex("#90E0EF"),
                FluidColor.FromHex("#CAF0F8")
            ],
            4 => // 薰衣草
            [
                FluidColor.FromHex("#7C3AED"),
                FluidColor.FromHex("#A78BFA"),
                FluidColor.FromHex("#C4B5FD"),
                FluidColor.FromHex("#DDD6FE")
            ],
            5 => // 樱花
            [
                FluidColor.FromHex("#ED3A7C"),
                FluidColor.FromHex("#FAB7A7"),
                FluidColor.FromHex("#FDB5C4"),
                FluidColor.FromHex("#FED6ED")
            ],
            _ => FluidConfig.DefaultColors
        };
        UpdateConfig(c => c.Colors = colors);
    }

    private void OnModeChanged(object sender, SelectionChangedEventArgs e)
    {
        var mode = ModeCombo.SelectedIndex switch
        {
            0 => FluidMode.Fluid,
            1 => FluidMode.Starfield,
            _ => FluidMode.Fluid
        };
        var showStarfieldOptions = mode == FluidMode.Starfield;
        MeteorCheckBox.Visibility = showStarfieldOptions ? Visibility.Visible : Visibility.Collapsed;
        NebulaCheckBox.Visibility = showStarfieldOptions ? Visibility.Visible : Visibility.Collapsed;
        QualityLabel.Visibility = showStarfieldOptions ? Visibility.Collapsed : Visibility.Visible;
        QualitySlider.Visibility = showStarfieldOptions ? Visibility.Collapsed : Visibility.Visible;
        UpdateConfig(c => c.Mode = mode);
    }

    private void OnSpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        SpeedLabel.Text = $"速度:{e.NewValue:F1}";
        UpdateConfig(c => c.Speed = (float)e.NewValue);
    }

    private void OnDensityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        DensityLabel.Text = $"浓度:{e.NewValue:F2}";
        UpdateConfig(c => c.Density = (float)e.NewValue);
    }

    private void OnQualityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
