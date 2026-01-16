using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DualAutoClicker.Services;
using DualAutoClicker.Native;
using DualAutoClicker.Views;
using Windows.UI;

namespace DualAutoClicker.Controls;

public sealed partial class SettingsPanel : UserControl
{
    private SettingsService? _settingsService;
    private bool _isBinding = false;
    private MouseHook? _bindingMouseHook;
    private KeyboardHook? _bindingKeyboardHook;

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

        MasterToggleCheckBox.IsChecked = settings.MasterToggle.Enabled;
        MasterKeyLabel.Text = settings.MasterToggle.KeyName;

        if (!settings.MasterToggle.Enabled)
        {
            App.ClickerService.SetMasterEnabled(true);
            UpdateMasterVisualState(true);
        }
        else
        {
            UpdateMasterVisualState(App.ClickerService.MasterEnabled);
        }


        var window = settings.WindowTarget;
        WindowStatusLabel.Text = window.Enabled && !string.IsNullOrEmpty(window.ProcessName)
            ? $"{window.ProcessName.Split(',').Length} uygulama seçili"
            : "Tüm Uygulamalar";

        StartupCheckBox.IsChecked = StartupService.IsStartupEnabled();
    }

    public void UpdateMasterState(bool enabled)
    {
        UpdateMasterVisualState(enabled);
    }

    private void UpdateMasterVisualState(bool enabled)
    {
        var brushKey = enabled ? "AccentOrangeBrush" : "AccentRedBrush";
        var colorKey = enabled ? "AccentOrangeColor" : "AccentRedColor";

        var brush = (Brush)Application.Current.Resources[brushKey];
        var color = (Color)Application.Current.Resources[colorKey];

        MasterKeyLabel.Foreground = brush;
        MasterKeyButton.Background = new SolidColorBrush(color);
        MasterKeyButton.Content = enabled ? "Seç" : "Durdu";
        MasterKeyButton.IsEnabled = enabled;
    }


    private void MasterToggleCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_settingsService == null) return;

        bool enabled = MasterToggleCheckBox.IsChecked == true;
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
        if (_isBinding)
        {
            StopKeyBinding();
            return;
        }

        StartKeyBinding();
    }

    private void StartKeyBinding()
    {
        _isBinding = true;
        MasterKeyButton.Content = "...";
        MasterKeyButton.Background = new SolidColorBrush(Color.FromArgb(255, 180, 130, 0));

        _bindingMouseHook = new MouseHook();
        _bindingKeyboardHook = new KeyboardHook();

        _bindingMouseHook.MouseButtonPressed += OnBindingMousePressed;
        _bindingKeyboardHook.KeyPressed += OnBindingKeyPressed;

        _bindingMouseHook.Install();
        _bindingKeyboardHook.Install();
    }

    private void StopKeyBinding()
    {
        _isBinding = false;

        _bindingMouseHook?.Uninstall();
        _bindingKeyboardHook?.Uninstall();

        if (_bindingMouseHook != null)
        {
            _bindingMouseHook.MouseButtonPressed -= OnBindingMousePressed;
            _bindingMouseHook.Dispose();
            _bindingMouseHook = null;
        }

        if (_bindingKeyboardHook != null)
        {
            _bindingKeyboardHook.KeyPressed -= OnBindingKeyPressed;
            _bindingKeyboardHook.Dispose();
            _bindingKeyboardHook = null;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            MasterKeyButton.Content = "Seç";
            MasterKeyButton.Background = new SolidColorBrush(Color.FromArgb(255, 255, 140, 0)); // Orange
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
                WindowStatusLabel.Text = "Tüm Uygulamalar";
            }
            else
            {
                window.Enabled = dialog.SelectedProcesses.Count > 0;
                window.ProcessName = string.Join(",", dialog.SelectedProcesses);
                WindowStatusLabel.Text = window.Enabled
                    ? $"{dialog.SelectedProcesses.Count} uygulama seçili"
                    : "Tüm Uygulamalar";
            }

            _settingsService.Save();
            App.ClickerService.UpdateWindowTargeting();
        }
    }

    private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_settingsService == null) return;

        bool enabled = StartupCheckBox.IsChecked == true;
        _settingsService.Settings.StartWithWindows = enabled;
        _settingsService.Save();
        StartupService.SetStartupEnabled(enabled);
    }
}
