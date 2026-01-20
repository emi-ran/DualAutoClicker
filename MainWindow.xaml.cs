using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DualAutoClicker.Models;
using DualAutoClicker.Services;
using DualAutoClicker.Native;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Windows.Graphics;
using Windows.UI;
using Windows.Foundation;

namespace DualAutoClicker;

public sealed partial class MainWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly SettingsService _settingsService;
    private readonly ClickerService _clickerService;
    private readonly Button[] _profileButtons = new Button[6];

    // For key binding
    private readonly MouseHook _bindingMouseHook;
    private readonly KeyboardHook _bindingKeyboardHook;

    public MainWindow()
    {
        this.InitializeComponent();

        _settingsService = App.SettingsService;
        _clickerService = App.ClickerService;

        // Get AppWindow for customization
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Configure window
        ConfigureWindow();
        SetupTitleBar();
        CreateProfileButtons();

        // Initialize binding hooks
        _bindingMouseHook = new MouseHook();
        _bindingKeyboardHook = new KeyboardHook();

        // Wire up events
        _settingsService.ProfileChanged += OnProfileChanged;
        _clickerService.ClickingStateChanged += OnClickingStateChanged;
        _clickerService.MasterStateChanged += OnMasterStateChanged;
        
        // Window events for drag rects
        this.Activated += MainWindow_Activated;
        this.SizeChanged += MainWindow_SizeChanged;
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
        if (AppWindowTitleBar.IsCustomizationSupported() && _appWindow.TitleBar.ExtendsContentIntoTitleBar && this.Content.XamlRoot != null)
        {
            try
            {
                double scale = this.Content.XamlRoot.RasterizationScale;
                var titleBar = AppTitleBar;
                
                // Profil butonlarının pozisyonunu bul
                var transform = ProfileButtons.TransformToVisual(null);
                var bounds = transform.TransformBounds(new Rect(0, 0, ProfileButtons.ActualWidth, ProfileButtons.ActualHeight));
                
                int titleBarHeight = (int)(titleBar.ActualHeight * scale);
                int profileX = (int)(bounds.X * scale);
                int profileWidth = (int)(bounds.Width * scale);
                int windowWidth = (int)(titleBar.ActualWidth * scale);

                var rects = new List<RectInt32>();

                // 1. Sol taraf (Logo dahil, butonlara kadar)
                // Eğer butonların solunda boşluk varsa orası sürüklenebilir olsun
                // Not: Logoya tıklanabilir olmasını istemiyorsak burayı drag alanı yaparız.
                if (profileX > 0)
                {
                    rects.Add(new RectInt32(0, 0, profileX, titleBarHeight));
                }

                // 2. Sağ taraf (Butonlardan sonra, pencere kontrollerine kadar)
                // Window controls (Minimize/Close) sağ üstte. Yaklaşık 100px (scale ile çarpılmalı)
                // SystemOverlayLeftInset ve SystemOverlayRightInset ile daha doğru hesaplanabilir ama şimdilik manuel.
                int rightInset = (int)(90 * scale); // Minimize + Close butonları
                
                int startX = profileX + profileWidth;
                int width = windowWidth - startX - rightInset;

                if (width > 0)
                {
                    rects.Add(new RectInt32(startX, 0, width, titleBarHeight));
                }

                _appWindow.TitleBar.SetDragRectangles(rects.ToArray());
            }
            catch
            {
                // UI henüz tam yüklenmemiş olabilir, ignore
            }
        }
    }

    private void ConfigureWindow()
    {
        // Set initial size - must be wide enough for both panels
        int windowWidth = 920;
        int windowHeight = 650;
        _appWindow.Resize(new SizeInt32(windowWidth, windowHeight));
        
        // Set window icon
        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        if (System.IO.File.Exists(iconPath))
        {
            _appWindow.SetIcon(iconPath);
        }

        // Center window on screen
        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var x = (workArea.Width - windowWidth) / 2;
        var y = (workArea.Height - windowHeight) / 2;
        _appWindow.Move(new PointInt32(x, y));

        // Set title
        _appWindow.Title = "Dual AutoClicker";
    }

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
            
            // Set button colors
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(30, 255, 255, 255);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(20, 255, 255, 255);
            titleBar.ButtonForegroundColor = Color.FromArgb(255, 140, 140, 160);
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

            btn.Click += (s, e) =>
            {
                _settingsService.SwitchProfile(index);
                UpdateProfileButtonStyles();
            };

            // Use ContextFlyout for right-click menu
            var flyout = new MenuFlyout();
            flyout.MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["DarkMenuFlyoutStyle"];

            var renameItem = new MenuFlyoutItem { Text = "Yeniden Adlandır" };
            renameItem.Style = (Style)Application.Current.Resources["DarkMenuFlyoutItemStyle"];
            renameItem.Click += (s, e) => ShowProfileRenameDialog(index);
            
            flyout.Items.Add(renameItem);
            btn.ContextFlyout = flyout;

            _profileButtons[i] = btn;
            ProfileButtons.Items.Add(btn);
        }
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
        }
    }

    private async void ShowProfileRenameDialog(int profileIndex)
    {
        var profile = _settingsService.Settings.Profiles[profileIndex];

        var textBox = new TextBox
        {
            Text = profile.Name,
            MaxLength = 12,
            PlaceholderText = "Profil adı"
        };

        var dialog = new ContentDialog
        {
            Title = "Profil Adını Değiştir",
            Content = textBox,
            PrimaryButtonText = "Kaydet",
            CloseButtonText = "İptal",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.Content.XamlRoot
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

        // Load general settings
        SettingsPanel.LoadSettings(_settingsService);
        RootGrid.Opacity = _clickerService.MasterEnabled ? 1.0 : 0.5;

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
        DispatcherQueue.TryEnqueue(() =>
        {
            // Update UI based on master state
            SettingsPanel.UpdateMasterState(enabled);
            RootGrid.Opacity = enabled ? 1.0 : 0.5;
        });
    }


    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        var presenter = _appWindow.Presenter as OverlappedPresenter;
        presenter?.Minimize();
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            if (presenter.State == OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
            }
            else
            {
                presenter.Maximize();
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Hide to tray instead of closing
        this.Hide();
    }

    public void Hide()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        ShowWindow(hWnd, SW_HIDE);
    }

    public void Show()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        ShowWindow(hWnd, SW_SHOW);
        SetForegroundWindow(hWnd);
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
}
