using DualAutoClicker.Models;
using DualAutoClicker.Native;
using DualAutoClicker.Services;
using DualAutoClicker.Forms;
using System.Drawing.Drawing2D;

namespace DualAutoClicker;

public partial class MainForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly ClickerService _clickerService;
    private readonly NotifyIcon _trayIcon;
    private readonly Icon _activeIcon;
    private readonly Icon _inactiveIcon;
    private readonly Icon _disabledIcon;
    private readonly ToolTip _toolTip;

    // For key binding
    private readonly MouseHook _bindingMouseHook;
    private readonly KeyboardHook _bindingKeyboardHook;
    private Button? _currentBindingButton;
    private Label? _currentBindingLabel;
    private Action<string, int, string>? _onKeyBound;

    // UI Controls - Left
    private CheckBox _leftEnabledCheckBox = null!;
    private Button _leftKeyButton = null!;
    private Label _leftKeyLabel = null!;
    private RadioButton _leftHoldRadio = null!;
    private RadioButton _leftToggleRadio = null!;
    private NumericUpDown _leftCpsNumeric = null!;
    private NumericUpDown _leftRandomNumeric = null!;

    // UI Controls - Right
    private CheckBox _rightEnabledCheckBox = null!;
    private Button _rightKeyButton = null!;
    private Label _rightKeyLabel = null!;
    private RadioButton _rightHoldRadio = null!;
    private RadioButton _rightToggleRadio = null!;
    private NumericUpDown _rightCpsNumeric = null!;
    private NumericUpDown _rightRandomNumeric = null!;

    // UI Controls - Settings
    private CheckBox _masterToggleCheckBox = null!;
    private Button _masterKeyButton = null!;
    private Label _masterKeyLabel = null!;
    private Button _windowPickerButton = null!;
    private Label _windowStatusLabel = null!;
    private CheckBox _startupCheckBox = null!;

    // Colors
    private static readonly Color BgDark = Color.FromArgb(18, 18, 22);
    private static readonly Color BgPanel = Color.FromArgb(28, 28, 35);
    private static readonly Color BgInput = Color.FromArgb(38, 38, 45);
    private static readonly Color AccentBlue = Color.FromArgb(0, 150, 255);
    private static readonly Color AccentGreen = Color.FromArgb(0, 200, 120);
    private static readonly Color AccentOrange = Color.FromArgb(255, 150, 50);
    private static readonly Color TextPrimary = Color.FromArgb(240, 240, 245);
    private static readonly Color TextSecondary = Color.FromArgb(150, 150, 160);

    public MainForm()
    {
        _settingsService = new SettingsService();
        _settingsService.Load();

        _clickerService = new ClickerService(_settingsService);

        // Create status icons
        _activeIcon = CreateStatusIcon(Color.LimeGreen);
        _inactiveIcon = CreateStatusIcon(Color.DodgerBlue);
        _disabledIcon = CreateStatusIcon(Color.Gray);

        // Key binding hooks
        _bindingMouseHook = new MouseHook();
        _bindingKeyboardHook = new KeyboardHook();
        _bindingMouseHook.MouseButtonPressed += OnBindingMousePressed;
        _bindingKeyboardHook.KeyPressed += OnBindingKeyPressed;

        // Clicker events
        _clickerService.ClickingStateChanged += OnClickingStateChanged;
        _clickerService.MasterStateChanged += OnMasterStateChanged;

        _toolTip = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 500,
            ReshowDelay = 500,
            ShowAlways = true
        };

        InitializeComponent();

        // Load icon
        LoadApplicationIcon();

        // Setup tray
        _trayIcon = CreateTrayIcon();

        LoadSettingsToUI();
        _clickerService.Start();
    }

    private void LoadApplicationIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.ico");
        if (File.Exists(iconPath))
        {
            this.Icon = new Icon(iconPath);
        }
    }

    private NotifyIcon CreateTrayIcon()
    {
        var tray = new NotifyIcon
        {
            Icon = this.Icon ?? _inactiveIcon,
            Text = "Dual AutoClicker",
            Visible = false
        };
        tray.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; tray.Visible = false; };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Göster", null, (s, e) => { Show(); WindowState = FormWindowState.Normal; tray.Visible = false; });
        menu.Items.Add("-");
        menu.Items.Add("Çıkış", null, (s, e) => Application.Exit());
        tray.ContextMenuStrip = menu;

        return tray;
    }

    private Icon CreateStatusIcon(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 2, 2, 12, 12);
        return Icon.FromHandle(bmp.GetHicon());
    }

    private void OnClickingStateChanged(bool isClicking)
    {
        BeginInvoke(() =>
        {
            if (_clickerService.MasterEnabled)
            {
                _trayIcon.Icon = isClicking ? _activeIcon : _inactiveIcon;
                _trayIcon.Text = isClicking ? "Dual AutoClicker - Aktif" : "Dual AutoClicker";
            }
        });
    }

    private void OnMasterStateChanged(bool enabled)
    {
        BeginInvoke(() =>
        {
            _trayIcon.Icon = enabled ? _inactiveIcon : _disabledIcon;
            _trayIcon.Text = enabled ? "Dual AutoClicker" : "Dual AutoClicker - Devre Dışı";
        });
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form
        this.Text = "Dual AutoClicker";
        this.Size = new Size(650, 480);
        this.MinimumSize = new Size(650, 480);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = BgDark;
        this.ForeColor = TextPrimary;
        this.Font = new Font("Segoe UI", 9.5f);
        this.DoubleBuffered = true;

        // Title
        var titleLabel = new Label
        {
            Text = "DUAL AUTOCLICKER",
            Location = new Point(0, 12),
            Size = new Size(650, 30),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = TextPrimary,
            TextAlign = ContentAlignment.MiddleCenter
        };
        this.Controls.Add(titleLabel);

        // Panels
        var leftPanel = CreateClickerPanel("SOL TIK", 15, 50, AccentBlue, true);
        var rightPanel = CreateClickerPanel("SAĞ TIK", 305, 50, AccentGreen, false);
        var settingsPanel = CreateSettingsPanel();

        this.Controls.Add(leftPanel);
        this.Controls.Add(rightPanel);
        this.Controls.Add(settingsPanel);

        // Extract controls
        ExtractPanelControls(leftPanel,
            out _leftEnabledCheckBox, out _leftKeyButton, out _leftKeyLabel,
            out _leftHoldRadio, out _leftToggleRadio, out _leftCpsNumeric, out _leftRandomNumeric);

        ExtractPanelControls(rightPanel,
            out _rightEnabledCheckBox, out _rightKeyButton, out _rightKeyLabel,
            out _rightHoldRadio, out _rightToggleRadio, out _rightCpsNumeric, out _rightRandomNumeric);

        // Wire events
        WireLeftEvents();
        WireRightEvents();
        WireSettingsEvents();

        this.ResumeLayout(false);
    }

    private Panel CreateClickerPanel(string title, int x, int y, Color accent, bool isLeft)
    {
        var panel = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(280, 195),
            BackColor = BgPanel
        };

        // Add rounded corners effect with paint
        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(accent, 2);
            e.Graphics.DrawLine(pen, 0, 0, panel.Width - 1, 0);
        };

        int yPos = 12;

        // Title + Checkbox
        var enabledCheck = new CheckBox
        {
            Text = title,
            Location = new Point(15, yPos),
            Size = new Size(250, 28),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = accent,
            Tag = "enabled"
        };
        panel.Controls.Add(enabledCheck);
        yPos += 38;

        // Activation row
        var actLabel = new Label
        {
            Text = "Aktivasyon",
            Location = new Point(15, yPos + 4),
            Size = new Size(80, 22),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9)
        };
        panel.Controls.Add(actLabel);

        var keyLabel = new Label
        {
            Text = isLeft ? "MB4" : "MB5",
            Location = new Point(100, yPos + 4),
            Size = new Size(80, 22),
            ForeColor = accent,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Tag = "keylabel"
        };
        panel.Controls.Add(keyLabel);

        var keyBtn = CreateButton("SEÇ", 190, yPos, 75, 28, accent);
        keyBtn.Tag = "keybtn";
        panel.Controls.Add(keyBtn);
        yPos += 38;

        // Mode row
        var modeLabel = new Label
        {
            Text = "Mod",
            Location = new Point(15, yPos + 4),
            Size = new Size(40, 22),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9)
        };
        panel.Controls.Add(modeLabel);

        var holdRadio = new RadioButton
        {
            Text = "Basılı Tut",
            Location = new Point(60, yPos),
            Size = new Size(95, 26),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9),
            Tag = "hold"
        };
        panel.Controls.Add(holdRadio);

        var toggleRadio = new RadioButton
        {
            Text = "Toggle",
            Location = new Point(160, yPos),
            Size = new Size(90, 26),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9),
            Tag = "toggle"
        };
        panel.Controls.Add(toggleRadio);
        yPos += 35;

        // CPS row
        var cpsLabel = new Label
        {
            Text = "CPS",
            Location = new Point(15, yPos + 4),
            Size = new Size(35, 22),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9)
        };
        panel.Controls.Add(cpsLabel);

        var cpsNum = CreateNumeric(55, yPos, 60, 1, 100, isLeft ? 16 : 33);
        cpsNum.Tag = "cps";
        panel.Controls.Add(cpsNum);

        var randLabel = new Label
        {
            Text = "Rnd",
            Location = new Point(125, yPos + 4),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9)
        };
        _toolTip.SetToolTip(randLabel, "Rastgele: CPS'e varyasyon ekleyerek daha doğal tıklama");
        panel.Controls.Add(randLabel);

        var randNum = CreateNumeric(160, yPos, 50, 0, 30, 0);
        randNum.Tag = "random";
        panel.Controls.Add(randNum);

        var percentLabel = new Label
        {
            Text = "%",
            Location = new Point(213, yPos + 4),
            Size = new Size(20, 22),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9)
        };
        panel.Controls.Add(percentLabel);

        return panel;
    }

    private Panel CreateSettingsPanel()
    {
        var panel = new Panel
        {
            Location = new Point(15, 255),
            Size = new Size(605, 170),
            BackColor = BgPanel
        };

        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(AccentOrange, 2);
            e.Graphics.DrawLine(pen, 0, 0, panel.Width - 1, 0);
        };

        // Title
        var titleLabel = new Label
        {
            Text = "AYARLAR",
            Location = new Point(15, 10),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = AccentOrange
        };
        panel.Controls.Add(titleLabel);

        int yPos = 42;

        // Master Switch Section (Row 1)
        _masterToggleCheckBox = new CheckBox
        {
            Text = "Master Kontrol",
            Location = new Point(15, yPos),
            AutoSize = true,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _toolTip.SetToolTip(_masterToggleCheckBox, "Tüm makroları tek bir tuşla anında açıp kapatın (Acil Durum Anahtarı)");
        panel.Controls.Add(_masterToggleCheckBox);

        _masterKeyLabel = new Label
        {
            Text = "F8",
            Location = new Point(170, yPos + 2),
            Size = new Size(60, 22),
            ForeColor = AccentOrange,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        panel.Controls.Add(_masterKeyLabel);

        _masterKeyButton = CreateButton("SEÇ", 240, yPos - 3, 55, 28, AccentOrange);
        _toolTip.SetToolTip(_masterKeyButton, "Master kontrol tuşunu değiştirmek için tıklayın");
        panel.Controls.Add(_masterKeyButton);

        yPos += 45;

        // Window Targeting Section (Row 2)
        _windowPickerButton = CreateButton("UYGULAMA HEDEFLE", 15, yPos - 3, 140, 28, Color.FromArgb(80, 80, 90));
        _toolTip.SetToolTip(_windowPickerButton, "Otomatik tıklayıcının sadece belirli bir uygulamada çalışmasını sağlar");
        panel.Controls.Add(_windowPickerButton);

        _windowStatusLabel = new Label
        {
            Text = "Tüm uygulamalar (Global)",
            Location = new Point(165, yPos + 2),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9)
        };
        panel.Controls.Add(_windowStatusLabel);

        yPos += 45;

        // Startup & Info Section (Row 3)
        _startupCheckBox = new CheckBox
        {
            Text = "Windows ile başlat",
            Location = new Point(15, yPos),
            AutoSize = true,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9)
        };
        _toolTip.SetToolTip(_startupCheckBox, "Bilgisayar açıldığında uygulamayı otomatik olarak başlatır");
        panel.Controls.Add(_startupCheckBox);

        var infoLabel = new Label
        {
            Text = "💡 İpucu: Uygulama simge durumuna küçültüldüğünde \nsistem tepsisinde (saat yanı) çalışmaya devam eder.",
            Location = new Point(180, yPos - 5),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 100, 100),
            Font = new Font("Segoe UI", 8.5f)
        };
        panel.Controls.Add(infoLabel);

        return panel;
    }

    private Button CreateButton(string text, int x, int y, int w, int h, Color accent)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(accent, 0.2f);
        return btn;
    }

    private NumericUpDown CreateNumeric(int x, int y, int w, int min, int max, int value)
    {
        return new NumericUpDown
        {
            Location = new Point(x, y),
            Size = new Size(w, 28),
            Minimum = min,
            Maximum = max,
            Value = value,
            BackColor = BgInput,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private void ExtractPanelControls(Panel panel,
        out CheckBox enabled, out Button keyBtn, out Label keyLabel,
        out RadioButton hold, out RadioButton toggle,
        out NumericUpDown cps, out NumericUpDown random)
    {
        enabled = panel.Controls.OfType<CheckBox>().First(c => c.Tag?.ToString() == "enabled");
        keyBtn = panel.Controls.OfType<Button>().First(c => c.Tag?.ToString() == "keybtn");
        keyLabel = panel.Controls.OfType<Label>().First(c => c.Tag?.ToString() == "keylabel");
        hold = panel.Controls.OfType<RadioButton>().First(c => c.Tag?.ToString() == "hold");
        toggle = panel.Controls.OfType<RadioButton>().First(c => c.Tag?.ToString() == "toggle");
        cps = panel.Controls.OfType<NumericUpDown>().First(c => c.Tag?.ToString() == "cps");
        random = panel.Controls.OfType<NumericUpDown>().First(c => c.Tag?.ToString() == "random");
    }

    private void StartKeyBinding(Button button, Label label, Action<string, int, string> onBound)
    {
        _currentBindingButton = button;
        _currentBindingLabel = label;
        _onKeyBound = onBound;

        button.Text = "...";
        button.BackColor = Color.FromArgb(120, 100, 0);

        _bindingMouseHook.Install();
        _bindingKeyboardHook.Install();
    }

    private void StopKeyBinding()
    {
        _bindingMouseHook.Uninstall();
        _bindingKeyboardHook.Uninstall();

        if (_currentBindingButton != null)
        {
            _currentBindingButton.Text = "SEÇ";
            _currentBindingButton.BackColor = AccentBlue;
        }

        _currentBindingButton = null;
        _currentBindingLabel = null;
        _onKeyBound = null;
    }

    private void OnBindingMousePressed(int code, string name)
    {
        if (_onKeyBound == null) return;
        BeginInvoke(() => { _onKeyBound("mouse", code, name); StopKeyBinding(); });
    }

    private void OnBindingKeyPressed(int code, string name)
    {
        if (_onKeyBound == null) return;
        if (code == 0x1B) { BeginInvoke(StopKeyBinding); return; }
        BeginInvoke(() => { _onKeyBound("keyboard", code, name); StopKeyBinding(); });
    }

    private void WireLeftEvents()
    {
        var s = _settingsService.Settings.LeftClick;

        _leftEnabledCheckBox.CheckedChanged += (_, _) => { s.Enabled = _leftEnabledCheckBox.Checked; _settingsService.Save(); };

        _leftKeyButton.Click += (_, _) => StartKeyBinding(_leftKeyButton, _leftKeyLabel, (t, c, n) =>
        {
            s.KeyType = t; s.KeyCode = c; s.KeyName = n;
            _leftKeyLabel.Text = n;
            _settingsService.Save();
        });

        _leftHoldRadio.CheckedChanged += (_, _) => { if (_leftHoldRadio.Checked) { s.Mode = ActivationMode.Hold; _settingsService.Save(); } };
        _leftToggleRadio.CheckedChanged += (_, _) => { if (_leftToggleRadio.Checked) { s.Mode = ActivationMode.Toggle; _settingsService.Save(); } };
        _leftCpsNumeric.ValueChanged += (_, _) => { s.Cps = (int)_leftCpsNumeric.Value; _settingsService.Save(); };
        _leftRandomNumeric.ValueChanged += (_, _) => { s.RandomPercent = (int)_leftRandomNumeric.Value; _settingsService.Save(); };
    }

    private void WireRightEvents()
    {
        var s = _settingsService.Settings.RightClick;

        _rightEnabledCheckBox.CheckedChanged += (_, _) => { s.Enabled = _rightEnabledCheckBox.Checked; _settingsService.Save(); };

        _rightKeyButton.Click += (_, _) => StartKeyBinding(_rightKeyButton, _rightKeyLabel, (t, c, n) =>
        {
            s.KeyType = t; s.KeyCode = c; s.KeyName = n;
            _rightKeyLabel.Text = n;
            _settingsService.Save();
        });

        _rightHoldRadio.CheckedChanged += (_, _) => { if (_rightHoldRadio.Checked) { s.Mode = ActivationMode.Hold; _settingsService.Save(); } };
        _rightToggleRadio.CheckedChanged += (_, _) => { if (_rightToggleRadio.Checked) { s.Mode = ActivationMode.Toggle; _settingsService.Save(); } };
        _rightCpsNumeric.ValueChanged += (_, _) => { s.Cps = (int)_rightCpsNumeric.Value; _settingsService.Save(); };
        _rightRandomNumeric.ValueChanged += (_, _) => { s.RandomPercent = (int)_rightRandomNumeric.Value; _settingsService.Save(); };
    }

    private void WireSettingsEvents()
    {
        var master = _settingsService.Settings.MasterToggle;
        var window = _settingsService.Settings.WindowTarget;

        _masterToggleCheckBox.CheckedChanged += (_, _) => { master.Enabled = _masterToggleCheckBox.Checked; _settingsService.Save(); };

        _masterKeyButton.Click += (_, _) => StartKeyBinding(_masterKeyButton, _masterKeyLabel, (t, c, n) =>
        {
            master.KeyType = t; master.KeyCode = c; master.KeyName = n;
            _masterKeyLabel.Text = n;
            _masterKeyButton.BackColor = AccentOrange;
            _settingsService.Save();
        });

        _windowPickerButton.Click += (_, _) =>
        {
            using var dialog = new WindowPickerDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (dialog.AllApps)
                {
                    window.Enabled = false;
                    window.ProcessName = "";
                    _windowStatusLabel.Text = "Tüm uygulamalar";
                }
                else
                {
                    window.Enabled = dialog.SelectedProcesses.Count > 0;
                    window.ProcessName = string.Join(",", dialog.SelectedProcesses);
                    _windowStatusLabel.Text = window.Enabled
                        ? $"{dialog.SelectedProcesses.Count} uygulama"
                        : "Tüm uygulamalar";
                }
                _settingsService.Save();
                _clickerService.UpdateWindowTargeting();
            }
        };

        _startupCheckBox.CheckedChanged += (_, _) =>
        {
            _settingsService.Settings.StartWithWindows = _startupCheckBox.Checked;
            _settingsService.Save();
            StartupService.SetStartupEnabled(_startupCheckBox.Checked);
        };
    }

    private void LoadSettingsToUI()
    {
        var left = _settingsService.Settings.LeftClick;
        _leftEnabledCheckBox.Checked = left.Enabled;
        _leftKeyLabel.Text = left.KeyName;
        _leftHoldRadio.Checked = left.Mode == ActivationMode.Hold;
        _leftToggleRadio.Checked = left.Mode == ActivationMode.Toggle;
        _leftCpsNumeric.Value = Math.Clamp(left.Cps, 1, 100);
        _leftRandomNumeric.Value = Math.Clamp(left.RandomPercent, 0, 30);

        var right = _settingsService.Settings.RightClick;
        _rightEnabledCheckBox.Checked = right.Enabled;
        _rightKeyLabel.Text = right.KeyName;
        _rightHoldRadio.Checked = right.Mode == ActivationMode.Hold;
        _rightToggleRadio.Checked = right.Mode == ActivationMode.Toggle;
        _rightCpsNumeric.Value = Math.Clamp(right.Cps, 1, 100);
        _rightRandomNumeric.Value = Math.Clamp(right.RandomPercent, 0, 30);

        var master = _settingsService.Settings.MasterToggle;
        _masterToggleCheckBox.Checked = master.Enabled;
        _masterKeyLabel.Text = master.KeyName;

        var window = _settingsService.Settings.WindowTarget;
        _windowStatusLabel.Text = window.Enabled && !string.IsNullOrEmpty(window.ProcessName)
            ? $"{window.ProcessName.Split(',').Length} uygulama"
            : "Tüm uygulamalar";

        _startupCheckBox.Checked = StartupService.IsStartupEnabled();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
            _trayIcon.Visible = true;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopKeyBinding();
        _bindingMouseHook.Dispose();
        _bindingKeyboardHook.Dispose();
        _clickerService.Stop();
        _clickerService.Dispose();
        _trayIcon.Dispose();
        _activeIcon.Dispose();
        _inactiveIcon.Dispose();
        _disabledIcon.Dispose();
        _toolTip.Dispose();
        base.OnFormClosing(e);
    }
}
