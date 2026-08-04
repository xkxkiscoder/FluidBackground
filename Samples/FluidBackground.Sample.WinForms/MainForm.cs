using FluidBackground.Core.Models;
using FluidBackground.WinForms;

namespace FluidBackground.Sample.WinForms;

public partial class MainForm : Form
{
    private readonly TabControl _tabs;
    private readonly TabPage _fluidTab;
    private readonly TabPage _arcTab;

    // —— 流体背景控件 ——
    private readonly FluidBackground.WinForms.FluidBackgroundControl _previewControl;
    private readonly ComboBox _colorCombo;
    private readonly ComboBox _modeCombo;
    private readonly ComboBox _renderModeCombo;
    private readonly Label _speedLabel;
    private readonly TrackBar _speedSlider;
    private readonly Label _densityLabel;
    private readonly TrackBar _densitySlider;
    private readonly TrackBar _qualitySlider;
    private readonly Label _qualityLabel;
    private readonly CheckBox _meteorCheckBox;
    private readonly CheckBox _nebulaCheckBox;
    private readonly Label _starScaleLabel;
    private readonly TrackBar _starScaleSlider;

    // —— 闪电电弧控件 ——
    private readonly FluidBackground.WinForms.LightningArcProgressControl _arcControl;
    private readonly ComboBox _themeCombo;
    private readonly TrackBar _progressSlider;
    private readonly Label _progressLabel;

    public MainForm()
    {
        _tabs = new TabControl();
        _fluidTab = new TabPage();
        _arcTab = new TabPage();

        _previewControl = new FluidBackground.WinForms.FluidBackgroundControl();
        _colorCombo = new ComboBox();
        _modeCombo = new ComboBox();
        _renderModeCombo = new ComboBox();
        _speedLabel = new Label();
        _speedSlider = new TrackBar();
        _densityLabel = new Label();
        _densitySlider = new TrackBar();
        _qualitySlider = new TrackBar();
        _qualityLabel = new Label();
        _meteorCheckBox = new CheckBox();
        _nebulaCheckBox = new CheckBox();
        _starScaleLabel = new Label();
        _starScaleSlider = new TrackBar();

        _arcControl = new FluidBackground.WinForms.LightningArcProgressControl();
        _themeCombo = new ComboBox();
        _progressSlider = new TrackBar();
        _progressLabel = new Label();

        InitializeComponent();
        InitializePreview();
        BindEvents();
    }

    private void InitializeComponent()
    {
        Text = "FluidBackground 控件预览";
        Size = new Size(760, 660);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(30, 30, 40);

        // Tab 容器
        _tabs.Dock = DockStyle.Fill;
        _tabs.Padding = new Point(12, 8);
        Controls.Add(_tabs);

        // ============ Tab 1: 流体背景 ============
        _fluidTab.Text = "流体背景";
        _tabs.TabPages.Add(_fluidTab);

        // 预览控件
        _previewControl.Dock = DockStyle.Top;
        _previewControl.Height = 220;
        _fluidTab.Controls.Add(_previewControl);

        // 配色方案标签
        var colorLabel = new Label
        {
            Text = "配色",
            ForeColor = Color.FromArgb(160, 160, 180),
            Font = new Font("Microsoft YaHei", 9),
            Location = new Point(20, 240),
            AutoSize = true
        };
        _fluidTab.Controls.Add(colorLabel);

        // 配色方案下拉框
        _colorCombo.Location = new Point(60, 237);
        _colorCombo.Width = 200;
        _colorCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _fluidTab.Controls.Add(_colorCombo);

        // 动画模式标签
        var modeLabel = new Label
        {
            Text = "动画",
            ForeColor = Color.FromArgb(160, 160, 180),
            Font = new Font("Microsoft YaHei", 9),
            Location = new Point(20, 280),
            AutoSize = true
        };
        _fluidTab.Controls.Add(modeLabel);

        // 动画模式下拉框
        _modeCombo.Location = new Point(60, 277);
        _modeCombo.Width = 120;
        _modeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _fluidTab.Controls.Add(_modeCombo);

        // 渲染模式标签
        var renderLabel = new Label
        {
            Text = "渲染",
            ForeColor = Color.FromArgb(160, 160, 180),
            Font = new Font("Microsoft YaHei", 9),
            Location = new Point(200, 280),
            AutoSize = true
        };
        _fluidTab.Controls.Add(renderLabel);

        // 渲染模式下拉框（硬编码）
        _renderModeCombo.Location = new Point(240, 277);
        _renderModeCombo.Width = 120;
        _renderModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _renderModeCombo.Items.AddRange(new object[] { "2D", "3D", "自动" });
        _renderModeCombo.SelectedIndex = 0;
        _fluidTab.Controls.Add(_renderModeCombo);

        // 速度标签
        _speedLabel.Text = "速度:1.0";
        _speedLabel.ForeColor = Color.FromArgb(160, 160, 180);
        _speedLabel.Font = new Font("Microsoft YaHei", 9);
        _speedLabel.Location = new Point(20, 320);
        _speedLabel.AutoSize = true;
        _fluidTab.Controls.Add(_speedLabel);

        // 速度滑块
        _speedSlider.Location = new Point(60, 315);
        _speedSlider.Width = 620;
        _speedSlider.Minimum = 1;
        _speedSlider.Maximum = 30;
        _speedSlider.Value = 10;
        _speedSlider.TickFrequency = 1;
        _fluidTab.Controls.Add(_speedSlider);

        // 精度标签
        _qualityLabel.Text = "精度:1.0x";
        _qualityLabel.ForeColor = Color.FromArgb(160, 160, 180);
        _qualityLabel.Font = new Font("Microsoft YaHei", 9);
        _qualityLabel.Location = new Point(20, 360);
        _qualityLabel.AutoSize = true;
        _fluidTab.Controls.Add(_qualityLabel);

        // 精度滑块
        _qualitySlider.Location = new Point(60, 355);
        _qualitySlider.Width = 620;
        _qualitySlider.Minimum = 10;
        _qualitySlider.Maximum = 40;
        _qualitySlider.Value = 10;
        _qualitySlider.TickFrequency = 1;
        _fluidTab.Controls.Add(_qualitySlider);

        // 浓度标签
        _densityLabel.Text = "浓度:0.30";
        _densityLabel.ForeColor = Color.FromArgb(160, 160, 180);
        _densityLabel.Font = new Font("Microsoft YaHei", 9);
        _densityLabel.Location = new Point(20, 400);
        _densityLabel.AutoSize = true;
        _fluidTab.Controls.Add(_densityLabel);

        // 浓度滑块
        _densitySlider.Location = new Point(60, 395);
        _densitySlider.Width = 620;
        _densitySlider.Minimum = 10;
        _densitySlider.Maximum = 100;
        _densitySlider.Value = 30;
        _densitySlider.TickFrequency = 1;
        _fluidTab.Controls.Add(_densitySlider);

        // 星星大小标签
        _starScaleLabel.Text = "星星大小:1.0";
        _starScaleLabel.ForeColor = Color.FromArgb(160, 160, 180);
        _starScaleLabel.Font = new Font("Microsoft YaHei", 9);
        _starScaleLabel.Location = new Point(20, 440);
        _starScaleLabel.AutoSize = true;
        _starScaleLabel.Visible = false;
        _fluidTab.Controls.Add(_starScaleLabel);

        // 星星大小滑块
        _starScaleSlider.Location = new Point(100, 435);
        _starScaleSlider.Width = 580;
        _starScaleSlider.Minimum = 1;
        _starScaleSlider.Maximum = 30;
        _starScaleSlider.Value = 10;
        _starScaleSlider.TickFrequency = 1;
        _starScaleSlider.Visible = false;
        _fluidTab.Controls.Add(_starScaleSlider);

        // 流星复选框（仅星空模式显示）
        _meteorCheckBox.Text = "流星";
        _meteorCheckBox.Checked = true;
        _meteorCheckBox.Visible = false;
        _meteorCheckBox.ForeColor = Color.FromArgb(160, 160, 180);
        _meteorCheckBox.Location = new Point(60, 480);
        _meteorCheckBox.AutoSize = true;
        _fluidTab.Controls.Add(_meteorCheckBox);

        // 星云复选框（仅星空模式显示）
        _nebulaCheckBox.Text = "星云";
        _nebulaCheckBox.Checked = true;
        _nebulaCheckBox.Visible = false;
        _nebulaCheckBox.ForeColor = Color.FromArgb(160, 160, 180);
        _nebulaCheckBox.Location = new Point(150, 480);
        _nebulaCheckBox.AutoSize = true;
        _fluidTab.Controls.Add(_nebulaCheckBox);

        // ============ Tab 2: 闪电电弧进度 ============
        _arcTab.Text = "闪电电弧进度";
        _tabs.TabPages.Add(_arcTab);

        // 标题
        var titleLabel = new Label
        {
            Text = "闪电电弧进度边界",
            ForeColor = Color.FromArgb(230, 230, 240),
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            Location = new Point(24, 16),
            AutoSize = true
        };
        _arcTab.Controls.Add(titleLabel);

        // 预览卡片
        _arcControl.Location = new Point(24, 56);
        _arcControl.Size = new Size(680, 110);
        _arcTab.Controls.Add(_arcControl);

        // 主题选择
        var themeLabel = new Label
        {
            Text = "主题",
            ForeColor = Color.FromArgb(160, 160, 180),
            Font = new Font("Microsoft YaHei", 9),
            Location = new Point(24, 190),
            AutoSize = true
        };
        _arcTab.Controls.Add(themeLabel);

        _themeCombo.Location = new Point(64, 187);
        _themeCombo.Width = 280;
        _themeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeCombo.Items.AddRange(new object[]
        {
            "Blue White Cyan（科技冷峻）",
            "Purple Navy（神秘能量）",
            "Clean（危险警告）",
            "Green Yellow（生化变异）"
        });
        _themeCombo.SelectedIndex = 0;
        _arcTab.Controls.Add(_themeCombo);

        // 进度调节
        var progressLabel = new Label
        {
            Text = "进度",
            ForeColor = Color.FromArgb(160, 160, 180),
            Font = new Font("Microsoft YaHei", 9),
            Location = new Point(370, 190),
            AutoSize = true
        };
        _arcTab.Controls.Add(progressLabel);

        _progressSlider.Location = new Point(410, 185);
        _progressSlider.Width = 200;
        _progressSlider.Minimum = 0;
        _progressSlider.Maximum = 100;
        _progressSlider.Value = 30;
        _progressSlider.TickFrequency = 10;
        _arcTab.Controls.Add(_progressSlider);

        _progressLabel.Text = "30%";
        _progressLabel.ForeColor = Color.FromArgb(0, 245, 255);
        _progressLabel.Font = new Font("Consolas", 12, FontStyle.Bold);
        _progressLabel.Location = new Point(618, 188);
        _progressLabel.AutoSize = true;
        _arcTab.Controls.Add(_progressLabel);

        // 说明
        var hintLabel = new Label
        {
            Text = "拖拽卡片或滑动进度条。进度达到 100% 时电弧脉冲增强后稳定；\r\n拖动时抖动增大，松开后约 0.5 秒衰减回原值。",
            ForeColor = Color.FromArgb(110, 110, 130),
            Font = new Font("Microsoft YaHei", 9),
            Location = new Point(24, 230),
            AutoSize = true
        };
        _arcTab.Controls.Add(hintLabel);
    }

    private void InitializePreview()
    {
        // 初始化动画模式列表
        InitializeModeList();

        // 初始化颜色列表
        UpdateColorList(FluidMode.Fluid);

        // 使用默认颜色预设初始化
        var defaultConfig = FluidPresets.CreateConfig(GeneralColorPreset.DeepSea);
        defaultConfig.EnablePointerInteraction = true;
        _previewControl.Config = defaultConfig;

        _arcControl.Config = new LightningArcConfig
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
        _modeCombo.Items.Clear();
        foreach (var mode in FluidPresets.RenderModes)
        {
            _modeCombo.Items.Add(mode.Name);
        }
        _modeCombo.SelectedIndex = 0;
    }

    private void BindEvents()
    {
        _modeCombo.SelectedIndexChanged += OnModeChanged;
        _speedSlider.ValueChanged += OnSpeedChanged;
        _densitySlider.ValueChanged += OnDensityChanged;
        _qualitySlider.ValueChanged += OnQualityChanged;
        _renderModeCombo.SelectedIndexChanged += OnRenderModeChanged;
        _meteorCheckBox.CheckedChanged += OnMeteorChanged;
        _nebulaCheckBox.CheckedChanged += OnNebulaChanged;
        _starScaleSlider.ValueChanged += OnStarScaleChanged;

        _themeCombo.SelectedIndexChanged += OnThemeChanged;
        _progressSlider.ValueChanged += OnProgressChanged;
    }

    private void OnColorChanged(object? sender, EventArgs e)
    {
        if (_colorCombo.SelectedIndex < 0)
            return;

        var mode = GetCurrentMode();
        var colors = FluidPresets.GetColors(mode, _colorCombo.SelectedIndex);
        UpdateConfig(c => c.Colors = colors);
    }

    private void OnModeChanged(object? sender, EventArgs e)
    {
        if (_modeCombo.SelectedIndex < 0)
            return;

        var mode = GetCurrentMode();
        var isStarfield = mode == FluidMode.Starfield;
        var isNebulaOrAurora = mode == FluidMode.Nebula || mode == FluidMode.Aurora;

        // 星空模式选项
        _meteorCheckBox.Visible = isStarfield;
        _nebulaCheckBox.Visible = isStarfield;
        _starScaleLabel.Visible = isStarfield;
        _starScaleSlider.Visible = isStarfield;

        // 精度选项（星空和星云/极光模式隐藏）
        _qualityLabel.Visible = !isStarfield && !isNebulaOrAurora;
        _qualitySlider.Visible = !isStarfield && !isNebulaOrAurora;

        // 浓度选项（星云/极光模式隐藏，因为这两种效果不使用浓度）
        _densityLabel.Visible = !isNebulaOrAurora;
        _densitySlider.Visible = !isNebulaOrAurora;

        // 更新颜色列表
        UpdateColorList(mode);

        UpdateConfig(c => c.Mode = mode);
    }

    private void UpdateColorList(FluidMode mode)
    {
        var colorNames = FluidPresets.GetColorNames(mode);
        _colorCombo.Items.Clear();
        foreach (var name in colorNames)
        {
            _colorCombo.Items.Add(name);
        }
        _colorCombo.SelectedIndex = 0;
    }

    private FluidMode GetCurrentMode()
    {
        if (_modeCombo.SelectedIndex < 0)
            return FluidMode.Fluid;

        return _modeCombo.SelectedIndex switch
        {
            0 => FluidMode.Fluid,
            1 => FluidMode.Starfield,
            2 => FluidMode.Nebula,
            3 => FluidMode.Aurora,
            _ => FluidMode.Fluid
        };
    }

    private void OnSpeedChanged(object? sender, EventArgs e)
    {
        _speedLabel.Text = $"速度:{_speedSlider.Value / 10.0f:F1}";
        UpdateConfig(c => c.Speed = _speedSlider.Value / 10.0f);
    }

    private void OnDensityChanged(object? sender, EventArgs e)
    {
        _densityLabel.Text = $"浓度:{_densitySlider.Value / 100.0f:F2}";
        UpdateConfig(c => c.Density = _densitySlider.Value / 100.0f);
    }

    private void OnQualityChanged(object? sender, EventArgs e)
    {
        var quality = _qualitySlider.Value / 10.0f;
        _qualityLabel.Text = $"精度:{quality:F1}x";
        UpdateConfig(c => c.RenderQuality = quality);
    }

    private void OnMeteorChanged(object? sender, EventArgs e)
    {
        UpdateConfig(c => c.EnableMeteor = _meteorCheckBox.Checked);
    }

    private void OnNebulaChanged(object? sender, EventArgs e)
    {
        UpdateConfig(c => c.EnableNebula = _nebulaCheckBox.Checked);
    }

    private void OnStarScaleChanged(object? sender, EventArgs e)
    {
        _starScaleLabel.Text = $"星星大小:{_starScaleSlider.Value / 10.0f:F1}";
        UpdateConfig(c => c.StarScale = _starScaleSlider.Value / 10.0f);
    }

    private void OnRenderModeChanged(object? sender, EventArgs e)
    {
        var mode = _renderModeCombo.SelectedIndex switch
        {
            0 => RenderMode.Force2D,
            1 => RenderMode.Force3D,
            2 => RenderMode.Auto,
            _ => RenderMode.Auto
        };
        UpdateConfig(c => c.RenderMode = mode);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_arcControl.Config is not LightningArcConfig config)
            return;

        var theme = _themeCombo.SelectedIndex switch
        {
            1 => LightningArcTheme.PurpleNavy,
            2 => LightningArcTheme.Clean,
            3 => LightningArcTheme.GreenYellow,
            _ => LightningArcTheme.BlueWhiteCyan
        };

        var newConfig = config.Clone();
        newConfig.Theme = theme;
        _arcControl.Config = newConfig;
    }

    private void OnProgressChanged(object? sender, EventArgs e)
    {
        _progressLabel.Text = $"{_progressSlider.Value}%";
        _arcControl.Progress = _progressSlider.Value;
    }

    private void UpdateConfig(Action<FluidConfig> update)
    {
        if (_previewControl.Config is FluidConfig config)
        {
            var newConfig = config.Clone();
            update(newConfig);
            _previewControl.Config = newConfig;
        }
    }
}
