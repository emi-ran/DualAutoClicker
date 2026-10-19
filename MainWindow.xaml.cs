using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using DualAutoClicker.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Windows.Graphics;
using Windows.UI;

namespace DualAutoClicker;

public sealed partial class MainWindow : Window
{
    private const double StackClickersBelowWidth = 840;
    private const double HideProfileHintBelowWidth = 980;
    private const int MinimumWindowWidth = 760;
    private const int MinimumWindowHeight = 560;
    private const uint WmGetMinMaxInfo = 0x0024;
    private static readonly UIntPtr WindowSubclassId = new(1);

    private readonly AppWindow _appWindow;
    private readonly SettingsService _settingsService;
    private readonly ClickerService _clickerService;
    private readonly Button[] _profileButtons = new Button[6];
    private readonly IntPtr _windowHandle;
    private readonly SubclassProc _windowSubclassProc;

    private bool _isClickersStacked;

    public MainWindow()
    {
        this.InitializeComponent();

        _settingsService = App.SettingsService;
        _clickerService = App.ClickerService;

        // Get AppWindow for customization
        _windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_windowHandle);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _windowSubclassProc = WindowSubclassCallback;
        SetWindowSubclass(_windowHandle, _windowSubclassProc, WindowSubclassId, UIntPtr.Zero);

        // Configure window
        ConfigureWindow();
        SetupTitleBar();
        CreateProfileButtons();

        // Wire up events
        _settingsService.ProfileChanged += OnProfileChanged;
        _clickerService.ClickingStateChanged += OnClickingStateChanged;
        _clickerService.MasterStateChanged += OnMasterStateChanged;
        _appWindow.Changed += (_, _) => UpdateDragRects();

        // Window events for drag rects
        this.Activated += MainWindow_Activated;
        this.SizeChanged += MainWindow_SizeChanged;
        ClickersGrid.SizeChanged += (_, args) => ApplyResponsiveLayout(args.NewSize.Width);
        this.Closed += (_, _) =>
        {
            RemoveWindowSubclass(_windowHandle, _windowSubclassProc, WindowSubclassId);
            App.Shutdown();
        };
        AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;

        // Load settings to UI
        LoadSettingsToUI();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        UpdateDragRects();
    }

    private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        UpdateDragRects();
    }

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRects();
    }

    private void UpdateDragRects()
    {
        if (!AppWindowTitleBar.IsCustomizationSupported() ||
            !_appWindow.TitleBar.ExtendsContentIntoTitleBar ||
            this.Content.XamlRoot == null)
        {
            return;
        }

        try
        {
            double scale = this.Content.XamlRoot.RasterizationScale;
            var titleBar = AppTitleBar;
            var systemTitleBar = _appWindow.TitleBar;

            double leftInset = systemTitleBar.LeftInset / scale;
            double rightInset = systemTitleBar.RightInset / scale;
            LeftCaptionInsetColumn.Width = new GridLength(leftInset);
            RightCaptionInsetColumn.Width = new GridLength(rightInset);

            int titleBarHeight = (int)Math.Round(titleBar.ActualHeight * scale);
            int dragX = (int)Math.Round(leftInset * scale);
            int dragWidth = (int)Math.Round(Math.Max(0, (titleBar.ActualWidth - leftInset - rightInset) * scale));

            if (titleBarHeight > 0 && dragWidth > 0)
            {
                systemTitleBar.SetDragRectangles(new[] { new RectInt32(dragX, 0, dragWidth, titleBarHeight) });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UpdateDragRects failed: {ex}");
        }
    }

    private void ApplyResponsiveLayout(double width)
    {
        bool stacked = width < StackClickersBelowWidth;

        if (stacked != _isClickersStacked)
        {
            _isClickersStacked = stacked;

            if (stacked)
            {
                ClickersGrid.ColumnDefinitions[1].Width = new GridLength(0);
                ClickersGrid.ColumnSpacing = 0;
                Grid.SetColumn(RightClickPanel, 0);
                Grid.SetRow(RightClickPanel, 1);
            }
            else
            {
                ClickersGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                ClickersGrid.ColumnSpacing = 16;
                Grid.SetColumn(RightClickPanel, 1);
                Grid.SetRow(RightClickPanel, 0);
            }
        }

        ProfileHint.Visibility = width >= HideProfileHintBelowWidth
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ConfigureWindow()
    {
        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        // Keep the window inside the work area on small displays.
        int windowWidth = Math.Min(1080, Math.Max(MinimumWindowWidth, workArea.Width - 80));
        int windowHeight = Math.Min(760, Math.Max(MinimumWindowHeight, workArea.Height - 80));

        _appWindow.Resize(new SizeInt32(windowWidth, windowHeight));

        // Set window icon
        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        if (System.IO.File.Exists(iconPath))
        {
            _appWindow.SetIcon(iconPath);
        }

        // Center window on screen
        var x = workArea.X + (workArea.Width - windowWidth) / 2;
        var y = workArea.Y + (workArea.Height - windowHeight) / 2;
        _appWindow.Move(new PointInt32(x, y));

        _appWindow.Title = "Dual AutoClicker";
    }

    private IntPtr WindowSubclassCallback(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr subclassId,
        UIntPtr referenceData)
    {
        if (message == WmGetMinMaxInfo && lParam != IntPtr.Zero)
        {
            var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            double scale = GetDpiForWindow(hWnd) / 96d;
            minMaxInfo.MinTrackSize.X = Math.Max(minMaxInfo.MinTrackSize.X, (int)Math.Ceiling(MinimumWindowWidth * scale));
            minMaxInfo.MinTrackSize.Y = Math.Max(minMaxInfo.MinTrackSize.Y, (int)Math.Ceiling(MinimumWindowHeight * scale));
            Marshal.StructureToPtr(minMaxInfo, lParam, false);
            return IntPtr.Zero;
        }

        return DefSubclassProc(hWnd, message, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    private delegate IntPtr SubclassProc(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr subclassId,
        UIntPtr referenceData);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc subclassProc, UIntPtr subclassId, UIntPtr referenceData);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc subclassProc, UIntPtr subclassId);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    private void SetupTitleBar()
    {
        // Extend content into title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Configure title bar colors
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = _appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(24, 255, 255, 255);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(16, 255, 255, 255);
            titleBar.ButtonForegroundColor = Color.FromArgb(255, 168, 178, 191);
            titleBar.ButtonHoverForegroundColor = Colors.White;
        }
    }

    private void CreateProfileButtons()
    {
        ProfileButtons.Items.Clear();
        int activeIndex = _settingsService.Settings.ActiveProfileIndex;

        for (int i = 0; i < 6; i++)
        {
            int index = i; // Capture for closure
            var profile = _settingsService.Settings.Profiles[i];

            var btn = new Button
            {
                Content = profile.Name,
                Style = index == activeIndex
                    ? (Style)Application.Current.Resources["ActiveProfileButtonStyle"]
                    : (Style)Application.Current.Resources["ProfileButtonStyle"],
                Tag = index
            };

            AutomationProperties.SetName(btn, $"{profile.Name}, profil {index + 1}");
            AutomationProperties.SetHelpText(btn, index == activeIndex ? "Seçili profil" : "Profili seç");
            ToolTipService.SetToolTip(btn, "Sol tık: profili seç · Sağ tık: yeniden adlandır");

            btn.Click += (s, e) =>
            {
                _settingsService.SwitchProfile(index);
                UpdateProfileButtonStyles();
            };

            // Use a lazily created flyout: building six MenuFlyouts (with styles)
            // during startup costs more than the rest of the window construction.
            btn.RightTapped += ProfileButton_RightTapped;

            _profileButtons[i] = btn;
            ProfileButtons.Items.Add(btn);
        }
    }

    private void ProfileButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int index) return;

        var flyout = new MenuFlyout
        {
            MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["DarkMenuFlyoutStyle"]
        };

        var renameItem = new MenuFlyoutItem
        {
            Text = "Yeniden Adlandır",
            Style = (Style)Application.Current.Resources["DarkMenuFlyoutItemStyle"]
        };
        renameItem.Click += (_, _) => ShowProfileRenameDialog(index);
        flyout.Items.Add(renameItem);

        flyout.ShowAt(button, new FlyoutShowOptions { Position = e.GetPosition(button) });
        e.Handled = true;
    }

    private void UpdateProfileButtonStyles()
    {
        int activeIndex = _settingsService.Settings.ActiveProfileIndex;

        for (int i = 0; i < 6; i++)
        {
            var btn = _profileButtons[i];
            if (btn == null) continue;

            var profile = _settingsService.Settings.Profiles[i];
            btn.Content = profile.Name;
            btn.Style = i == activeIndex
                ? (Style)Application.Current.Resources["ActiveProfileButtonStyle"]
                : (Style)Application.Current.Resources["ProfileButtonStyle"];

            AutomationProperties.SetName(btn, $"{profile.Name}, profil {i + 1}");
            AutomationProperties.SetHelpText(btn, i == activeIndex ? "Seçili profil" : "Profili seç");
        }
    }

    private async void ShowProfileRenameDialog(int profileIndex)
    {
        var profile = _settingsService.Settings.Profiles[profileIndex];

        var textBox = new TextBox
        {
            Text = profile.Name,
            MaxLength = 12,
            PlaceholderText = "Profil adı",
            MinWidth = 300,
            Style = (Style)Application.Current.Resources["FormTextBoxStyle"]
        };
        AutomationProperties.SetName(textBox, "Profil adı");

        var dialog = new ContentDialog
        {
            Title = "Profil Adını Değiştir",
            Content = textBox,
            PrimaryButtonText = "Kaydet",
            CloseButtonText = "İptal",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.Content.XamlRoot,
            Background = ThemeBrush("BgElevatedBrush"),
            BorderBrush = ThemeBrush("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
            CloseButtonStyle = (Style)Application.Current.Resources["SecondaryButtonStyle"]
        };

        dialog.Opened += (_, _) =>
        {
            textBox.Focus(FocusState.Programmatic);
            textBox.SelectAll();
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            _settingsService.UpdateProfileName(profileIndex, textBox.Text.Trim());
            UpdateProfileButtonStyles();
        }
    }

    private void LoadSettingsToUI()
    {
        // Load left click settings
        LeftClickPanel.LoadSettings(_settingsService.Settings.LeftClick);

        // Load right click settings
        RightClickPanel.LoadSettings(_settingsService.Settings.RightClick);

        // Load macro settings
        MacroPanel.LoadSettings(_settingsService);

        // Load general settings
        SettingsPanel.LoadSettings(_settingsService);
        UpdateMasterVisuals(_clickerService.MasterEnabled);
    }

    private void OnProfileChanged(int profileIndex)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            LoadSettingsToUI();
            UpdateProfileButtonStyles();
        });
    }

    private void OnClickingStateChanged(bool isClicking)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // Update status indicators
            LeftClickPanel.UpdateClickingState(_clickerService.IsLeftClicking);
            RightClickPanel.UpdateClickingState(_clickerService.IsRightClicking);
        });
    }

    private void OnMasterStateChanged(bool enabled)
    {
        DispatcherQueue.TryEnqueue(() => UpdateMasterVisuals(enabled));
    }

    private void UpdateMasterVisuals(bool enabled)
    {
        MasterStatusText.Text = enabled ? "Etkin" : "Duraklatıldı";
        MasterStatusDot.Fill = ThemeBrush(enabled ? "SuccessBrush" : "WarningBrush");
        MasterBanner.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
        ContentRoot.Opacity = enabled ? 1.0 : 0.55;

        AutomationProperties.SetName(MasterStatusPill, $"Sistem durumu: {(enabled ? "etkin" : "duraklatıldı")}");

        SettingsPanel.UpdateMasterState(enabled);
    }

    private static Brush ThemeBrush(string key) => (Brush)Application.Current.Resources[key];
}
