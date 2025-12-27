using DualAutoClicker.Models;
using DualAutoClicker.Native;
using DualAutoClicker.Services;

namespace DualAutoClicker;

public partial class MainForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly ClickerService _clickerService;
    private readonly NotifyIcon _trayIcon;

    // For key binding
    private readonly MouseHook _bindingMouseHook;
    private readonly KeyboardHook _bindingKeyboardHook;
    private Button? _currentBindingButton;
    private Action<string, int, string>? _onKeyBound;

    // Left click controls
    private CheckBox _leftEnabledCheckBox = null!;
    private Button _leftKeyButton = null!;
    private Label _leftKeyLabel = null!;
    private RadioButton _leftHoldRadio = null!;
    private RadioButton _leftToggleRadio = null!;
    private NumericUpDown _leftCpsNumeric = null!;

    // Right click controls
    private CheckBox _rightEnabledCheckBox = null!;
    private Button _rightKeyButton = null!;
    private Label _rightKeyLabel = null!;
    private RadioButton _rightHoldRadio = null!;
    private RadioButton _rightToggleRadio = null!;
    private NumericUpDown _rightCpsNumeric = null!;

    public MainForm()
    {
        _settingsService = new SettingsService();
        _settingsService.Load();

        _clickerService = new ClickerService(_settingsService);

        // Separate hooks for key binding (not for activation)
        _bindingMouseHook = new MouseHook();
        _bindingKeyboardHook = new KeyboardHook();
        _bindingMouseHook.MouseButtonPressed += OnBindingMousePressed;
        _bindingKeyboardHook.KeyPressed += OnBindingKeyPressed;

        InitializeComponent();

        // Load custom icon
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.ico");
        Icon? customIcon = null;
        if (File.Exists(iconPath))
        {
            customIcon = new Icon(iconPath);
            this.Icon = customIcon;
        }

        // Setup tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = customIcon ?? SystemIcons.Application,
            Text = "Dual AutoClicker",
            Visible = false
        };
        _trayIcon.DoubleClick += TrayIcon_DoubleClick;

        // Load settings into UI
        LoadSettingsToUI();

        // Start clicker service
        _clickerService.Start();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form settings
        this.Text = "Dual AutoClicker";
        this.Size = new Size(520, 280);
        this.MinimumSize = new Size(520, 280);
        this.MaximumSize = new Size(520, 280);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;

        // Left Click Panel
        var leftPanel = CreateClickPanel("SOL TIK", 10, 10, true);
        this.Controls.Add(leftPanel);

        // Right Click Panel
        var rightPanel = CreateClickPanel("SAĞ TIK", 260, 10, false);
        this.Controls.Add(rightPanel);

        // Extract controls from panels
        ExtractControlsFromPanel(leftPanel, out _leftEnabledCheckBox, out _leftKeyButton, out _leftKeyLabel,
            out _leftHoldRadio, out _leftToggleRadio, out _leftCpsNumeric);
        ExtractControlsFromPanel(rightPanel, out _rightEnabledCheckBox, out _rightKeyButton, out _rightKeyLabel,
            out _rightHoldRadio, out _rightToggleRadio, out _rightCpsNumeric);

        // Wire up events
        WireUpLeftClickEvents();
        WireUpRightClickEvents();

        this.ResumeLayout(false);
    }

    private GroupBox CreateClickPanel(string title, int x, int y, bool isLeft)
    {
        var panel = new GroupBox
        {
            Text = title,
            Location = new Point(x, y),
            Size = new Size(240, 220),
            ForeColor = Color.FromArgb(100, 180, 255),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        int yOffset = 25;

        // Enabled checkbox
        var enabledCheckBox = new CheckBox
        {
            Text = "Aktif",
            Location = new Point(15, yOffset),
            Size = new Size(200, 25),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            Tag = "enabled"
        };
        panel.Controls.Add(enabledCheckBox);
        yOffset += 30;

        // Activation key label
        var keyTitleLabel = new Label
        {
            Text = "Aktivasyon:",
            Location = new Point(15, yOffset + 5),
            Size = new Size(75, 20),
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 9)
        };
        panel.Controls.Add(keyTitleLabel);

        // Key display label
        var keyLabel = new Label
        {
            Text = isLeft ? "MB4" : "MB5",
            Location = new Point(90, yOffset + 5),
            Size = new Size(60, 20),
            ForeColor = Color.Cyan,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Tag = "keylabel"
        };
        panel.Controls.Add(keyLabel);

        // Select button
        var keyButton = new Button
        {
            Text = "Seç",
            Location = new Point(155, yOffset),
            Size = new Size(65, 28),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            Tag = "keybutton",
            Cursor = Cursors.Hand
        };
        keyButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
        panel.Controls.Add(keyButton);
        yOffset += 38;

        // Mode label
        var modeLabel = new Label
        {
            Text = "Mod:",
            Location = new Point(15, yOffset + 3),
            Size = new Size(40, 20),
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 9)
        };
        panel.Controls.Add(modeLabel);

        // Hold radio
        var holdRadio = new RadioButton
        {
            Text = "Basılı tut",
            Location = new Point(60, yOffset),
            Size = new Size(85, 25),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            Tag = "hold"
        };
        panel.Controls.Add(holdRadio);

        // Toggle radio
        var toggleRadio = new RadioButton
        {
            Text = "Toggle",
            Location = new Point(145, yOffset),
            Size = new Size(80, 25),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            Tag = "toggle"
        };
        panel.Controls.Add(toggleRadio);
        yOffset += 35;

        // CPS label
        var cpsLabel = new Label
        {
            Text = "CPS:",
            Location = new Point(15, yOffset + 3),
            Size = new Size(40, 20),
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 9)
        };
        panel.Controls.Add(cpsLabel);

        // CPS numeric
        var cpsNumeric = new NumericUpDown
        {
            Location = new Point(60, yOffset),
            Size = new Size(80, 25),
            Minimum = 1,
            Maximum = 100,
            Value = 16,
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            Tag = "cps"
        };
        panel.Controls.Add(cpsNumeric);

        // CPS hint
        var cpsHint = new Label
        {
            Text = "tık/sn",
            Location = new Point(145, yOffset + 3),
            Size = new Size(50, 20),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8)
        };
        panel.Controls.Add(cpsHint);

        return panel;
    }

    private void ExtractControlsFromPanel(GroupBox panel,
        out CheckBox enabledCheckBox, out Button keyButton, out Label keyLabel,
        out RadioButton holdRadio, out RadioButton toggleRadio,
        out NumericUpDown cpsNumeric)
    {
        enabledCheckBox = panel.Controls.OfType<CheckBox>().First(c => c.Tag?.ToString() == "enabled");
        keyButton = panel.Controls.OfType<Button>().First(c => c.Tag?.ToString() == "keybutton");
        keyLabel = panel.Controls.OfType<Label>().First(c => c.Tag?.ToString() == "keylabel");
        holdRadio = panel.Controls.OfType<RadioButton>().First(c => c.Tag?.ToString() == "hold");
        toggleRadio = panel.Controls.OfType<RadioButton>().First(c => c.Tag?.ToString() == "toggle");
        cpsNumeric = panel.Controls.OfType<NumericUpDown>().First(c => c.Tag?.ToString() == "cps");
    }

    private void StartKeyBinding(Button button, Label label, Action<string, int, string> onBound)
    {
        // Visual feedback
        _currentBindingButton = button;
        _onKeyBound = onBound;
        button.Text = "...";
        button.BackColor = Color.FromArgb(80, 80, 0);

        // Start listening
        _bindingMouseHook.Install();
        _bindingKeyboardHook.Install();
    }

    private void StopKeyBinding()
    {
        _bindingMouseHook.Uninstall();
        _bindingKeyboardHook.Uninstall();

        if (_currentBindingButton != null)
        {
            _currentBindingButton.Text = "Seç";
            _currentBindingButton.BackColor = Color.FromArgb(60, 60, 60);
        }

        _currentBindingButton = null;
        _onKeyBound = null;
    }

    private void OnBindingMousePressed(int buttonCode, string buttonName)
    {
        if (_onKeyBound == null) return;

        this.BeginInvoke(() =>
        {
            _onKeyBound("mouse", buttonCode, buttonName);
            StopKeyBinding();
        });
    }

    private void OnBindingKeyPressed(int vkCode, string keyName)
    {
        if (_onKeyBound == null) return;

        // Ignore ESC - it cancels binding
        if (vkCode == 0x1B) // ESC
        {
            this.BeginInvoke(StopKeyBinding);
            return;
        }

        this.BeginInvoke(() =>
        {
            _onKeyBound("keyboard", vkCode, keyName);
            StopKeyBinding();
        });
    }

    private void WireUpLeftClickEvents()
    {
        _leftEnabledCheckBox.CheckedChanged += (s, e) =>
        {
            _settingsService.Settings.LeftClick.Enabled = _leftEnabledCheckBox.Checked;
            _settingsService.Save();
        };

        _leftKeyButton.Click += (s, e) =>
        {
            StartKeyBinding(_leftKeyButton, _leftKeyLabel, (type, code, name) =>
            {
                _settingsService.Settings.LeftClick.KeyType = type;
                _settingsService.Settings.LeftClick.KeyCode = code;
                _settingsService.Settings.LeftClick.KeyName = name;
                _settingsService.Save();
                _leftKeyLabel.Text = name;
            });
        };

        _leftHoldRadio.CheckedChanged += (s, e) =>
        {
            if (_leftHoldRadio.Checked)
            {
                _settingsService.Settings.LeftClick.Mode = ActivationMode.Hold;
                _settingsService.Save();
            }
        };

        _leftToggleRadio.CheckedChanged += (s, e) =>
        {
            if (_leftToggleRadio.Checked)
            {
                _settingsService.Settings.LeftClick.Mode = ActivationMode.Toggle;
                _settingsService.Save();
            }
        };

        _leftCpsNumeric.ValueChanged += (s, e) =>
        {
            _settingsService.Settings.LeftClick.Cps = (int)_leftCpsNumeric.Value;
            _settingsService.Save();
        };
    }

    private void WireUpRightClickEvents()
    {
        _rightEnabledCheckBox.CheckedChanged += (s, e) =>
        {
            _settingsService.Settings.RightClick.Enabled = _rightEnabledCheckBox.Checked;
            _settingsService.Save();
        };

        _rightKeyButton.Click += (s, e) =>
        {
            StartKeyBinding(_rightKeyButton, _rightKeyLabel, (type, code, name) =>
            {
                _settingsService.Settings.RightClick.KeyType = type;
                _settingsService.Settings.RightClick.KeyCode = code;
                _settingsService.Settings.RightClick.KeyName = name;
                _settingsService.Save();
                _rightKeyLabel.Text = name;
            });
        };

        _rightHoldRadio.CheckedChanged += (s, e) =>
        {
            if (_rightHoldRadio.Checked)
            {
                _settingsService.Settings.RightClick.Mode = ActivationMode.Hold;
                _settingsService.Save();
            }
        };

        _rightToggleRadio.CheckedChanged += (s, e) =>
        {
            if (_rightToggleRadio.Checked)
            {
                _settingsService.Settings.RightClick.Mode = ActivationMode.Toggle;
                _settingsService.Save();
            }
        };

        _rightCpsNumeric.ValueChanged += (s, e) =>
        {
            _settingsService.Settings.RightClick.Cps = (int)_rightCpsNumeric.Value;
            _settingsService.Save();
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

        var right = _settingsService.Settings.RightClick;
        _rightEnabledCheckBox.Checked = right.Enabled;
        _rightKeyLabel.Text = right.KeyName;
        _rightHoldRadio.Checked = right.Mode == ActivationMode.Hold;
        _rightToggleRadio.Checked = right.Mode == ActivationMode.Toggle;
        _rightCpsNumeric.Value = Math.Clamp(right.Cps, 1, 100);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
            _trayIcon.Visible = true;
            _trayIcon.ShowBalloonTip(1000, "Dual AutoClicker", "Sistem tepsisine küçültüldü", ToolTipIcon.Info);
        }
    }

    private void TrayIcon_DoubleClick(object? sender, EventArgs e)
    {
        Show();
        WindowState = FormWindowState.Normal;
        _trayIcon.Visible = false;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        StopKeyBinding();
        _bindingMouseHook.Dispose();
        _bindingKeyboardHook.Dispose();

        _clickerService.Stop();
        _clickerService.Dispose();
        _trayIcon.Dispose();
    }
}
