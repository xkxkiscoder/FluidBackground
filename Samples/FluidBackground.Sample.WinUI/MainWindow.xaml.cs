using FluidBackground.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluidBackground.Sample.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "流体背景预览";
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(540, 620));
        this.AppWindow.IsShownInSwitchers = true;
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
            Intensity = 0.7f,
            Mode = FluidMode.Fluid,
            EnablePointerInteraction = true
        };
    }

    private void BindEvents()
    {
        ColorCombo.SelectionChanged += OnColorChanged;
        ModeCombo.SelectionChanged += OnModeChanged;
        SpeedSlider.ValueChanged += OnSpeedChanged;
        IntensitySlider.ValueChanged += OnIntensityChanged;
        QualitySlider.ValueChanged += OnQualityChanged;
        RenderModeCombo.SelectionChanged += OnRenderModeChanged;
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
            1 => FluidMode.Ripple,
            2 => FluidMode.Breathing,
            _ => FluidMode.Fluid
        };
        UpdateConfig(c => c.Mode = mode);
    }

    private void OnSpeedChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        UpdateConfig(c => c.Speed = (float)e.NewValue);
    }

    private void OnIntensityChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        UpdateConfig(c => c.Intensity = (float)e.NewValue);
    }

    private void OnQualityChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        var quality = (float)e.NewValue;
        QualityLabel.Text = $"精度:{quality:F1}x";
        UpdateConfig(c => c.RenderQuality = quality);
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
