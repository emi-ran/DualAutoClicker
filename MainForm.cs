using DualAutoClicker.Models;
using DualAutoClicker.Native;
using DualAutoClicker.Services;
using DualAutoClicker.Forms;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace DualAutoClicker;

public partial class MainForm : Form
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    // For borderless form dragging
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    private readonly SettingsService _settingsService;
    private readonly ClickerService _clickerService;
    private readonly NotifyIcon _trayIcon;
    private Icon? _appIcon;
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

    // Animation system
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly Dictionary<Control, AnimationState> _animations = new();
    private float _glowPhase = 0f;
    private float _pulsePhase = 0f;

    // Status indicators
    private Panel _leftStatusIndicator = null!;
    private Panel _rightStatusIndicator = null!;
    private bool _leftClicking = false;
    private bool _rightClicking = false;

    // UI Controls - Left
    private CheckBox _leftEnabledCheckBox = null!;
    private Button _leftKeyButton = null!;
    private Label _leftKeyLabel = null!;
    private RadioButton _leftHoldRadio = null!;
    private RadioButton _leftToggleRadio = null!;
    private NumericUpDown _leftCpsNumeric = null!;
    private NumericUpDown _leftRandomNumeric = null!;
    private Panel _leftPanel = null!;

    // UI Controls - Right
    private CheckBox _rightEnabledCheckBox = null!;
    private Button _rightKeyButton = null!;
    private Label _rightKeyLabel = null!;
    private RadioButton _rightHoldRadio = null!;
    private RadioButton _rightToggleRadio = null!;
    private NumericUpDown _rightCpsNumeric = null!;
    private NumericUpDown _rightRandomNumeric = null!;
    private Panel _rightPanel = null!;

    // UI Controls - Settings
    private CheckBox _masterToggleCheckBox = null!;
    private Button _masterKeyButton = null!;
    private Label _masterKeyLabel = null!;
    private Button _windowPickerButton = null!;
    private Label _windowStatusLabel = null!;
    private CheckBox _startupCheckBox = null!;

    // Modern Color Palette
    private static readonly Color BgDark = Color.FromArgb(13, 13, 18);
    private static readonly Color BgCard = Color.FromArgb(22, 22, 30);
    private static readonly Color BgCardHover = Color.FromArgb(28, 28, 38);
    private static readonly Color BgInput = Color.FromArgb(35, 35, 45);
    private static readonly Color BorderColor = Color.FromArgb(45, 45, 60);

    // Gradient Colors
    private static readonly Color AccentCyan = Color.FromArgb(0, 210, 255);
    private static readonly Color AccentPurple = Color.FromArgb(148, 0, 255);
    private static readonly Color AccentPink = Color.FromArgb(255, 0, 128);
    private static readonly Color AccentGreen = Color.FromArgb(0, 255, 136);
    private static readonly Color AccentOrange = Color.FromArgb(255, 140, 0);

    private static readonly Color TextPrimary = Color.FromArgb(245, 245, 250);
    private static readonly Color TextSecondary = Color.FromArgb(140, 140, 160);
    private static readonly Color TextMuted = Color.FromArgb(90, 90, 110);

    private class AnimationState
    {
        public float CurrentValue { get; set; }
        public float TargetValue { get; set; }
        public float Speed { get; set; } = 0.15f;
    }

    public MainForm()
    {
        _settingsService = new SettingsService();
        _settingsService.Load();

        _clickerService = new ClickerService(_settingsService);

        LoadApplicationIcon();

        _activeIcon = CreateStatusIcon(AccentGreen);
        _inactiveIcon = CreateStatusIcon(AccentCyan);
        _disabledIcon = CreateStatusIcon(Color.Gray);

        _bindingMouseHook = new MouseHook();
        _bindingKeyboardHook = new KeyboardHook();
        _bindingMouseHook.MouseButtonPressed += OnBindingMousePressed;
        _bindingKeyboardHook.KeyPressed += OnBindingKeyPressed;

        _clickerService.ClickingStateChanged += OnClickingStateChanged;
        _clickerService.MasterStateChanged += OnMasterStateChanged;

        _toolTip = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 500,
            ReshowDelay = 500,
            ShowAlways = true,
            BackColor = BgCard,
            ForeColor = TextPrimary
        };

        // Animation timer for smooth effects
        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60fps
        _animationTimer.Tick += OnAnimationTick;

        InitializeComponent();

        _trayIcon = CreateTrayIcon();

        LoadSettingsToUI();
        _clickerService.Start();
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        _glowPhase += 0.03f;
        _pulsePhase += 0.05f;
        if (_glowPhase > Math.PI * 2) _glowPhase = 0;
        if (_pulsePhase > Math.PI * 2) _pulsePhase = 0;

        // Update status indicators
        _leftStatusIndicator?.Invalidate();
        _rightStatusIndicator?.Invalidate();

        // Animate control hover states
        foreach (var kvp in _animations)
        {
            var state = kvp.Value;
            if (Math.Abs(state.CurrentValue - state.TargetValue) > 0.01f)
            {
                state.CurrentValue += (state.TargetValue - state.CurrentValue) * state.Speed;
                kvp.Key.Invalidate();
            }
        }
    }

    private void LoadApplicationIcon()
    {
        try
        {
            using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("DualAutoClicker.icon.ico");
            if (stream != null)
            {
                _appIcon = new Icon(stream);
                this.Icon = _appIcon;
            }
        }
        catch
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.ico");
            if (File.Exists(iconPath))
            {
                _appIcon = new Icon(iconPath);
                this.Icon = _appIcon;
            }
        }
    }

    private NotifyIcon CreateTrayIcon()
    {
        var tray = new NotifyIcon
        {
            Icon = this.Icon ?? _inactiveIcon,
            Text = "Dual AutoClicker",
            Visible = true
        };
        tray.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; };

        var menu = new ContextMenuStrip();
        menu.BackColor = Color.FromArgb(30, 30, 35);
        menu.ForeColor = Color.White;
        menu.ShowImageMargin = false; // Soldaki beyaz alanı kaldır
        menu.Renderer = new DarkMenuRenderer(); // Özel renderer

        var showItem = new ToolStripMenuItem("Göster");
        showItem.Click += (s, e) => { Show(); WindowState = FormWindowState.Normal; };
        menu.Items.Add(showItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Çıkış");
        exitItem.Click += (s, e) => Application.Exit();
        menu.Items.Add(exitItem);

        tray.ContextMenuStrip = menu;

        return tray;
    }

    private Icon CreateStatusIcon(Color badgeColor)
    {
        const int size = 32;
        const int badgeSize = 12;
        var bmp = new Bitmap(size, size);

        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (_appIcon != null)
            {
                g.DrawIcon(_appIcon, new Rectangle(0, 0, size, size));
            }
            else
            {
                using var fallbackBrush = new SolidBrush(Color.FromArgb(60, 60, 70));
                g.FillRectangle(fallbackBrush, 2, 2, size - 4, size - 4);
            }

            int badgeX = size - badgeSize - 1;
            int badgeY = size - badgeSize - 1;

            using var outlineBrush = new SolidBrush(Color.FromArgb(30, 30, 35));
            g.FillEllipse(outlineBrush, badgeX - 1, badgeY - 1, badgeSize + 2, badgeSize + 2);

            using var badgeBrush = new SolidBrush(badgeColor);
            g.FillEllipse(badgeBrush, badgeX, badgeY, badgeSize, badgeSize);
        }

        IntPtr hIcon = bmp.GetHicon();
        Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        bmp.Dispose();
        return icon;
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

        // Borderless Form
        this.Text = "Dual AutoClicker";
        this.Size = new Size(700, 520);
        this.MinimumSize = new Size(700, 520);
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = BgDark;
        this.ForeColor = TextPrimary;
        this.Font = new Font("Segoe UI", 9.5f);
        this.DoubleBuffered = true;

        // Enable form dragging
        this.MouseDown += Form_MouseDown;

        // Custom title bar
        var titleBar = CreateTitleBar();
        this.Controls.Add(titleBar);

        // Main container with padding
        var mainContainer = new Panel
        {
            Location = new Point(20, 55),
            Size = new Size(660, 445),
            BackColor = Color.Transparent
        };
        this.Controls.Add(mainContainer);

        // Panels
        _leftPanel = CreateClickerPanel("SOL TIK", 0, 0, AccentCyan, AccentPurple, true);
        _rightPanel = CreateClickerPanel("SAĞ TIK", 340, 0, AccentPink, AccentOrange, false);
        var settingsPanel = CreateSettingsPanel();

        mainContainer.Controls.Add(_leftPanel);
        mainContainer.Controls.Add(_rightPanel);
        mainContainer.Controls.Add(settingsPanel);

        ExtractPanelControls(_leftPanel,
            out _leftEnabledCheckBox, out _leftKeyButton, out _leftKeyLabel,
            out _leftHoldRadio, out _leftToggleRadio, out _leftCpsNumeric, out _leftRandomNumeric,
            out _leftStatusIndicator);

        ExtractPanelControls(_rightPanel,
            out _rightEnabledCheckBox, out _rightKeyButton, out _rightKeyLabel,
            out _rightHoldRadio, out _rightToggleRadio, out _rightCpsNumeric, out _rightRandomNumeric,
            out _rightStatusIndicator);

        WireLeftEvents();
        WireRightEvents();
        WireSettingsEvents();

        this.ResumeLayout(false);
    }

    private void Form_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }
    }

    private Panel CreateTitleBar()
    {
        var titleBar = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(700, 50),
            BackColor = Color.Transparent
        };
        titleBar.MouseDown += Form_MouseDown;

        titleBar.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Gradient line at bottom
            using var lineBrush = new LinearGradientBrush(
                new Point(0, 49),
                new Point(700, 49),
                AccentCyan,
                AccentPurple);
            using var linePen = new Pen(lineBrush, 2);
            g.DrawLine(linePen, 0, 49, 700, 49);
        };

        // Logo/Title with gradient effect
        var titleLabel = new Label
        {
            Text = "⚡ DUAL AUTOCLICKER",
            Location = new Point(20, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = TextPrimary,
            BackColor = Color.Transparent
        };
        titleLabel.MouseDown += Form_MouseDown;
        titleBar.Controls.Add(titleLabel);

        // Window controls
        var closeBtn = CreateWindowButton("✕", 655, 10, () => Application.Exit());
        var minBtn = CreateWindowButton("─", 620, 10, () => WindowState = FormWindowState.Minimized);

        titleBar.Controls.Add(closeBtn);
        titleBar.Controls.Add(minBtn);

        return titleBar;
    }

    private Button CreateWindowButton(string text, int x, int y, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(35, 30),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10),
            ForeColor = TextSecondary,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 60);
        btn.Click += (s, e) => onClick();

        if (text == "✕")
        {
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 50, 50);
        }

        return btn;
    }

    private Panel CreateClickerPanel(string title, int x, int y, Color accent1, Color accent2, bool isLeft)
    {
        var panel = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(310, 220),
            BackColor = BgCard
        };

        // Rounded corners and gradient border
        panel.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Gradient top border
            using var borderBrush = new LinearGradientBrush(
                new Point(0, 0),
                new Point(panel.Width, 0),
                accent1,
                accent2);
            using var borderPen = new Pen(borderBrush, 3);
            g.DrawLine(borderPen, 0, 1, panel.Width, 1);

            // Side borders with fade
            using var sidePen = new Pen(Color.FromArgb(40, accent1), 1);
            g.DrawLine(sidePen, 0, 0, 0, panel.Height);
            g.DrawLine(sidePen, panel.Width - 1, 0, panel.Width - 1, panel.Height);
        };

        int yPos = 15;

        // Header with status indicator
        var headerPanel = new Panel
        {
            Location = new Point(15, yPos),
            Size = new Size(280, 35),
            BackColor = Color.Transparent
        };
        panel.Controls.Add(headerPanel);

        // Status indicator (animated glow)
        var statusIndicator = new Panel
        {
            Location = new Point(0, 8),
            Size = new Size(18, 18),
            BackColor = Color.Transparent,
            Tag = "status"
        };
        statusIndicator.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isActive = isLeft ? _leftClicking : _rightClicking;
            bool isEnabled = isLeft ? _leftEnabledCheckBox?.Checked == true : _rightEnabledCheckBox?.Checked == true;

            Color baseColor = isEnabled ? (isActive ? AccentGreen : accent1) : TextMuted;

            // Glow effect when active
            if (isActive)
            {
                float glowSize = 3 + (float)Math.Sin(_pulsePhase) * 2;
                using var glowBrush = new SolidBrush(Color.FromArgb(60, baseColor));
                g.FillEllipse(glowBrush, -glowSize, -glowSize, 18 + glowSize * 2, 18 + glowSize * 2);
            }

            // Main circle
            using var brush = new SolidBrush(baseColor);
            g.FillEllipse(brush, 2, 2, 14, 14);

            // Inner highlight
            using var highlightBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
            g.FillEllipse(highlightBrush, 4, 4, 6, 6);
        };
        headerPanel.Controls.Add(statusIndicator);

        // Title checkbox
        var enabledCheck = new CheckBox
        {
            Text = title,
            Location = new Point(25, 0),
            Size = new Size(200, 35),
            Font = new Font("Segoe UI Semibold", 14),
            ForeColor = TextPrimary,
            Tag = "enabled"
        };
        headerPanel.Controls.Add(enabledCheck);

        yPos += 45;

        // Activation key row
        var actLabel = new Label
        {
            Text = "Aktivasyon Tuşu",
            Location = new Point(15, yPos + 5),
            Size = new Size(110, 20),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9.5f)
        };
        panel.Controls.Add(actLabel);

        var keyLabel = new Label
        {
            Text = isLeft ? "MB4" : "MB5",
            Location = new Point(130, yPos + 2),
            Size = new Size(70, 28),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = accent1,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = BgInput,
            Tag = "keylabel"
        };
        panel.Controls.Add(keyLabel);

        var keyBtn = CreateModernButton("Değiştir", 210, yPos, 85, 32, accent1, accent2);
        keyBtn.Tag = "keybtn";
        panel.Controls.Add(keyBtn);
        yPos += 42;

        // Mode row
        var modeLabel = new Label
        {
            Text = "Mod",
            Location = new Point(15, yPos + 5),
            Size = new Size(40, 20),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9.5f)
        };
        panel.Controls.Add(modeLabel);

        var holdRadio = CreateModernRadio("Basılı Tut", 70, yPos, "hold");
        var toggleRadio = CreateModernRadio("Toggle", 190, yPos, "toggle");
        panel.Controls.Add(holdRadio);
        panel.Controls.Add(toggleRadio);
        yPos += 40;

        // CPS row with modern styling
        var cpsContainer = new Panel
        {
            Location = new Point(15, yPos),
            Size = new Size(280, 50),
            BackColor = BgInput
        };
        panel.Controls.Add(cpsContainer);

        // CPS Section
        var cpsLabel = new Label
        {
            Text = "CPS",
            Location = new Point(15, 15),
            Size = new Size(35, 20),
            ForeColor = accent1,
            Font = new Font("Segoe UI Semibold", 10)
        };
        cpsContainer.Controls.Add(cpsLabel);

        var cpsNum = CreateModernNumeric(55, 10, 60, 1, 100, isLeft ? 16 : 33);
        cpsNum.Tag = "cps";
        cpsContainer.Controls.Add(cpsNum);

        // Random Section - kısaltılmış "Rnd %"
        var randLabel = new Label
        {
            Text = "Rnd %",
            Location = new Point(130, 15),
            Size = new Size(50, 20),
            ForeColor = accent2,
            Font = new Font("Segoe UI Semibold", 10)
        };
        _toolTip.SetToolTip(randLabel, "Rastgele: CPS'e varyasyon ekler");
        cpsContainer.Controls.Add(randLabel);

        var randNum = CreateModernNumeric(185, 10, 55, 0, 30, 0);
        randNum.Tag = "random";
        cpsContainer.Controls.Add(randNum);

        return panel;
    }

    private Panel CreateSettingsPanel()
    {
        var panel = new Panel
        {
            Location = new Point(0, 235),
            Size = new Size(660, 210),
            BackColor = BgCard
        };

        panel.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Gradient top border
            using var borderBrush = new LinearGradientBrush(
                new Point(0, 0),
                new Point(panel.Width, 0),
                AccentOrange,
                AccentPink);
            using var borderPen = new Pen(borderBrush, 3);
            g.DrawLine(borderPen, 0, 1, panel.Width, 1);
        };

        // Title
        var titleLabel = new Label
        {
            Text = "⚙ AYARLAR",
            Location = new Point(20, 18),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 14),
            ForeColor = TextPrimary
        };
        panel.Controls.Add(titleLabel);

        int yPos = 60;

        // Master Toggle Section
        var masterContainer = new Panel
        {
            Location = new Point(20, yPos),
            Size = new Size(300, 50),
            BackColor = BgInput
        };
        panel.Controls.Add(masterContainer);

        var masterIcon = new Label
        {
            Text = "🔐",
            Location = new Point(12, 12),
            Size = new Size(25, 25),
            Font = new Font("Segoe UI", 14)
        };
        masterContainer.Controls.Add(masterIcon);

        _masterToggleCheckBox = new CheckBox
        {
            Text = "Master Kontrol",
            Location = new Point(40, 14),
            AutoSize = true,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 10)
        };
        _toolTip.SetToolTip(_masterToggleCheckBox, "Tüm makroları tek tuşla kontrol edin");
        masterContainer.Controls.Add(_masterToggleCheckBox);

        _masterKeyLabel = new Label
        {
            Text = "F8",
            Location = new Point(170, 10),
            Size = new Size(50, 30),
            ForeColor = AccentOrange,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = BgCard
        };
        masterContainer.Controls.Add(_masterKeyLabel);

        _masterKeyButton = CreateModernButton("Seç", 230, 10, 55, 30, AccentOrange, AccentPink);
        masterContainer.Controls.Add(_masterKeyButton);

        // Window Targeting Section
        var windowContainer = new Panel
        {
            Location = new Point(340, yPos),
            Size = new Size(300, 50),
            BackColor = BgInput
        };
        panel.Controls.Add(windowContainer);

        var windowIcon = new Label
        {
            Text = "🎯",
            Location = new Point(12, 12),
            Size = new Size(25, 25),
            Font = new Font("Segoe UI", 14)
        };
        windowContainer.Controls.Add(windowIcon);

        _windowPickerButton = CreateModernButton("Uygulama Seç", 40, 10, 115, 30, AccentCyan, AccentPurple);
        windowContainer.Controls.Add(_windowPickerButton);

        _windowStatusLabel = new Label
        {
            Text = "Tüm Uygulamalar",
            Location = new Point(165, 15),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9)
        };
        windowContainer.Controls.Add(_windowStatusLabel);

        yPos += 70;

        // Startup & Info Section
        var bottomContainer = new Panel
        {
            Location = new Point(20, yPos),
            Size = new Size(620, 45),
            BackColor = Color.Transparent
        };
        panel.Controls.Add(bottomContainer);

        _startupCheckBox = new CheckBox
        {
            Text = "  Windows ile başlat",
            Location = new Point(0, 10),
            AutoSize = true,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10)
        };
        bottomContainer.Controls.Add(_startupCheckBox);

        var infoLabel = new Label
        {
            Text = "💡 Küçültüldüğünde sistem tepsisinde çalışmaya devam eder",
            Location = new Point(200, 12),
            AutoSize = true,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9)
        };
        bottomContainer.Controls.Add(infoLabel);

        return panel;
    }

    private RadioButton CreateModernRadio(string text, int x, int y, string tag)
    {
        var radio = new RadioButton
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10),
            Tag = tag
        };
        return radio;
    }

    private Button CreateModernButton(string text, int x, int y, int w, int h, Color accent1, Color accent2)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9),
            ForeColor = Color.White,
            BackColor = accent1,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = accent2;

        // Add animation state
        _animations[btn] = new AnimationState();

        btn.MouseEnter += (s, e) =>
        {
            if (_animations.TryGetValue(btn, out var state))
                state.TargetValue = 1f;
        };
        btn.MouseLeave += (s, e) =>
        {
            if (_animations.TryGetValue(btn, out var state))
                state.TargetValue = 0f;
        };

        return btn;
    }

    private NumericUpDown CreateModernNumeric(int x, int y, int w, int min, int max, int value)
    {
        var num = new NumericUpDown
        {
            Location = new Point(x, y),
            Size = new Size(w, 30),
            Minimum = min,
            Maximum = max,
            Value = value,
            BackColor = BgCard,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.FixedSingle
        };
        return num;
    }

    private void ExtractPanelControls(Panel panel,
        out CheckBox enabled, out Button keyBtn, out Label keyLabel,
        out RadioButton hold, out RadioButton toggle,
        out NumericUpDown cps, out NumericUpDown random,
        out Panel statusIndicator)
    {
        // Find header panel first
        var headerPanel = panel.Controls.OfType<Panel>().FirstOrDefault(p => p.Location.Y < 30 && p.Size.Width > 100);

        enabled = headerPanel?.Controls.OfType<CheckBox>().FirstOrDefault(c => c.Tag?.ToString() == "enabled")
                  ?? panel.Controls.OfType<CheckBox>().First(c => c.Tag?.ToString() == "enabled");

        statusIndicator = headerPanel?.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "status")
                          ?? new Panel();

        keyBtn = panel.Controls.OfType<Button>().First(c => c.Tag?.ToString() == "keybtn");
        keyLabel = panel.Controls.OfType<Label>().First(c => c.Tag?.ToString() == "keylabel");
        hold = panel.Controls.OfType<RadioButton>().First(c => c.Tag?.ToString() == "hold");
        toggle = panel.Controls.OfType<RadioButton>().First(c => c.Tag?.ToString() == "toggle");

        // Find CPS container
        var cpsContainer = panel.Controls.OfType<Panel>().FirstOrDefault(p => p.BackColor == BgInput && p.Size.Height == 50);
        if (cpsContainer != null)
        {
            cps = cpsContainer.Controls.OfType<NumericUpDown>().First(c => c.Tag?.ToString() == "cps");
            random = cpsContainer.Controls.OfType<NumericUpDown>().First(c => c.Tag?.ToString() == "random");
        }
        else
        {
            cps = panel.Controls.OfType<NumericUpDown>().First(c => c.Tag?.ToString() == "cps");
            random = panel.Controls.OfType<NumericUpDown>().First(c => c.Tag?.ToString() == "random");
        }
    }

    private void StartKeyBinding(Button button, Label label, Action<string, int, string> onBound)
    {
        _currentBindingButton = button;
        _currentBindingLabel = label;
        _onKeyBound = onBound;

        button.Text = "...";
        button.BackColor = Color.FromArgb(180, 130, 0);

        _bindingMouseHook.Install();
        _bindingKeyboardHook.Install();
    }

    private void StopKeyBinding()
    {
        _bindingMouseHook.Uninstall();
        _bindingKeyboardHook.Uninstall();

        if (_currentBindingButton != null)
        {
            _currentBindingButton.Text = "Değiştir";
            _currentBindingButton.BackColor = AccentCyan;
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

        _leftEnabledCheckBox.CheckedChanged += (_, _) =>
        {
            s.Enabled = _leftEnabledCheckBox.Checked;
            _settingsService.Save();
            _leftStatusIndicator?.Invalidate();
        };

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

        // Track clicking state for animations
        _clickerService.LeftClickingChanged += (clicking) =>
        {
            _leftClicking = clicking;
            BeginInvoke(() => _leftStatusIndicator?.Invalidate());
        };
    }

    private void WireRightEvents()
    {
        var s = _settingsService.Settings.RightClick;

        _rightEnabledCheckBox.CheckedChanged += (_, _) =>
        {
            s.Enabled = _rightEnabledCheckBox.Checked;
            _settingsService.Save();
            _rightStatusIndicator?.Invalidate();
        };

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

        // Track clicking state for animations
        _clickerService.RightClickingChanged += (clicking) =>
        {
            _rightClicking = clicking;
            BeginInvoke(() => _rightStatusIndicator?.Invalidate());
        };
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
                    _windowStatusLabel.Text = "Tüm Uygulamalar";
                }
                else
                {
                    window.Enabled = dialog.SelectedProcesses.Count > 0;
                    window.ProcessName = string.Join(",", dialog.SelectedProcesses);
                    _windowStatusLabel.Text = window.Enabled
                        ? $"{dialog.SelectedProcesses.Count} uygulama seçili"
                        : "Tüm Uygulamalar";
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
            ? $"{window.ProcessName.Split(',').Length} uygulama seçili"
            : "Tüm Uygulamalar";

        _startupCheckBox.Checked = StartupService.IsStartupEnabled();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _animationTimer.Stop();
        _animationTimer.Dispose();
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

    // Override to remove form border shadow artifacts
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
            return cp;
        }
    }
}

/// <summary>
/// Custom renderer for dark themed context menu
/// </summary>
public class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkMenuColors()) { }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var rc = new Rectangle(Point.Empty, e.Item.Size);

        if (e.Item.Selected)
        {
            // Hover state - light background with readable text
            using var brush = new SolidBrush(Color.FromArgb(60, 60, 70));
            e.Graphics.FillRectangle(brush, rc);
        }
        else
        {
            // Normal state
            using var brush = new SolidBrush(Color.FromArgb(30, 30, 35));
            e.Graphics.FillRectangle(brush, rc);
        }
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using var pen = new Pen(Color.FromArgb(60, 60, 70));
        e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(60, 60, 70));
        var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
    }
}

public class DarkMenuColors : ProfessionalColorTable
{
    public override Color MenuItemSelected => Color.FromArgb(60, 60, 70);
    public override Color MenuItemBorder => Color.Transparent;
    public override Color MenuBorder => Color.FromArgb(60, 60, 70);
    public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 35);
    public override Color ImageMarginGradientBegin => Color.FromArgb(30, 30, 35);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 30, 35);
    public override Color ImageMarginGradientEnd => Color.FromArgb(30, 30, 35);
    public override Color SeparatorDark => Color.FromArgb(60, 60, 70);
    public override Color SeparatorLight => Color.FromArgb(60, 60, 70);
}

