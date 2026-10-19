using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DualAutoClicker.Services;
using DualAutoClicker.Views;

namespace DualAutoClicker.Controls;

public sealed partial class SettingsPanel : UserControl
{
    private SettingsService? _settingsService;
    private bool _isLoading;
    private readonly KeyBindingCapture _keyBindingCapture = new();

    public SettingsPanel()
    {
        this.InitializeComponent();
        this.Unloaded += SettingsPanel_Unloaded;
    }

    private void SettingsPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        StopKeyBinding();
    }

    public void LoadSettings(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var settings = settingsService.Settings;

        _isLoading = true;
        try
        {
            MasterToggleCheckBox.IsOn = settings.MasterToggle.Enabled;
            MasterKeyLabel.Text = settings.MasterToggle.KeyName;

            var window = settings.WindowTarget;
            WindowStatusLabel.Text = window.Enabled && !string.IsNullOrEmpty(window.ProcessName)
                ? $"{window.ProcessName.Split(',').Length} uygulama seçili"
                : "Tüm uygulamalar";

            StartupCheckBox.IsOn = StartupService.IsStartupEnabled();
        }
        finally
        {
            _isLoading = false;
        }

        if (!settings.MasterToggle.Enabled)
        {
            App.ClickerService.SetMasterEnabled(true);
            UpdateMasterVisualState(true);
        }
        else
        {
            UpdateMasterVisualState(App.ClickerService.MasterEnabled);
        }
    }

    public void UpdateMasterState(bool enabled)
    {
        UpdateMasterVisualState(enabled);
    }

    private void UpdateMasterVisualState(bool enabled)
    {
        MasterKeyLabel.Foreground = enabled ? ThemeBrush("AccentTextBrush") : ThemeBrush("WarningBrush");
        MasterKeyButton.IsEnabled = enabled;

        MasterStateDot.Fill = enabled ? ThemeBrush("SuccessBrush") : ThemeBrush("WarningBrush");
        MasterStateText.Foreground = enabled ? ThemeBrush("SuccessBrush") : ThemeBrush("WarningBrush");
        MasterStateText.Text = enabled ? "Etkin" : "Duraklatıldı";
    }

    private void MasterToggleCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _settingsService == null) return;

        bool enabled = MasterToggleCheckBox.IsOn;
        _settingsService.Settings.MasterToggle.Enabled = enabled;
        _settingsService.Save();

        if (!enabled)
        {
            App.ClickerService.SetMasterEnabled(true);
            UpdateMasterVisualState(true);
        }
        else
        {
            UpdateMasterVisualState(App.ClickerService.MasterEnabled);
        }
    }

    private void MasterKeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_keyBindingCapture.IsActive)
        {
            StopKeyBinding();
            return;
        }

        StartKeyBinding();
    }

    private void StartKeyBinding()
    {
        MasterKeyButton.Content = "Tuşa basın…";
        CaptureHint.Visibility = Visibility.Visible;
        _keyBindingCapture.Start(OnBindingMousePressed, OnBindingKeyPressed);
    }

    private void StopKeyBinding()
    {
        _keyBindingCapture.Stop();

        DispatcherQueue.TryEnqueue(() =>
        {
            MasterKeyButton.Content = "Değiştir";
            CaptureHint.Visibility = Visibility.Collapsed;
        });
    }

    private void OnBindingMousePressed(int code, string name)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_settingsService == null) return;

            var master = _settingsService.Settings.MasterToggle;
            master.KeyType = "mouse";
            master.KeyCode = code;
            master.KeyName = name;
            MasterKeyLabel.Text = name;
            _settingsService.Save();

            StopKeyBinding();
        });
    }

    private void OnBindingKeyPressed(int code, string name)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // Escape cancels binding
            if (code == 0x1B)
            {
                StopKeyBinding();
                return;
            }

            if (_settingsService == null) return;

            var master = _settingsService.Settings.MasterToggle;
            master.KeyType = "keyboard";
            master.KeyCode = code;
            master.KeyName = name;
            MasterKeyLabel.Text = name;
            _settingsService.Save();

            StopKeyBinding();
        });
    }

    private async void WindowPickerButton_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsService == null) return;

        var windowSettings = _settingsService.Settings.WindowTarget;
        var selectedProcesses = windowSettings.ProcessName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var dialog = new WindowPickerDialog(selectedProcesses, !windowSettings.Enabled)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var window = _settingsService.Settings.WindowTarget;

            if (dialog.AllApps)
            {
                window.Enabled = false;
                window.ProcessName = "";
                WindowStatusLabel.Text = "Tüm uygulamalar";
            }
            else
            {
                window.Enabled = dialog.SelectedProcesses.Count > 0;
                window.ProcessName = string.Join(",", dialog.SelectedProcesses);
                WindowStatusLabel.Text = window.Enabled
                    ? $"{dialog.SelectedProcesses.Count} uygulama seçili"
                    : "Tüm uygulamalar";
            }

            _settingsService.Save();
            App.ClickerService.UpdateWindowTargeting();
        }
    }

    private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _settingsService == null) return;

        bool enabled = StartupCheckBox.IsOn;
        _settingsService.Settings.StartWithWindows = enabled;
        _settingsService.Save();
        StartupService.SetStartupEnabled(enabled);
    }

    private static Brush ThemeBrush(string key) => (Brush)Application.Current.Resources[key];
}
