using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DualAutoClicker.Models;
using DualAutoClicker.Native;
using DualAutoClicker.Services;

namespace DualAutoClicker.Controls;

public sealed partial class MacroPanel : UserControl
{
    private KeyboardMacroSettings? _settings;
    private bool _isLoading;
    private readonly KeyBindingCapture _keyBindingCapture = new();

    public MacroPanel()
    {
        this.InitializeComponent();
        this.Unloaded += MacroPanel_Unloaded;
    }

    private void MacroPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        StopKeyBinding();
    }

    public void LoadSettings(SettingsService settingsService)
    {
        _isLoading = true;
        try
        {
            _settings = settingsService.Settings.KeyboardMacro;

            EnabledCheckBox.IsOn = _settings.Enabled;
            KeyLabel.Text = _settings.FullKeyName;
            BaseTextBox.Text = _settings.BaseText;
            MinCharsBox.Value = _settings.MinRandomChars;
            MaxCharsBox.Value = _settings.MaxRandomChars;
            JunkCharsBox.Text = _settings.JunkCharacters;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _settings == null) return;

        _settings.Enabled = EnabledCheckBox.IsOn;
        App.SettingsService.Save();
    }

    private void ChangeKeyButton_Click(object sender, RoutedEventArgs e)
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
        ChangeKeyButton.Content = "Tuşa basın…";
        CaptureHint.Visibility = Visibility.Visible;
        _keyBindingCapture.Start(OnBindingMousePressed, OnBindingKeyPressed);
    }

    private void StopKeyBinding()
    {
        _keyBindingCapture.Stop();

        DispatcherQueue.TryEnqueue(() =>
        {
            ChangeKeyButton.Content = "Seç";
            CaptureHint.Visibility = Visibility.Collapsed;
        });
    }

    private void OnBindingMousePressed(int code, string name)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_settings == null || _keyBindingCapture.KeyboardHook == null) return;

            // Capture current modifier states from keyboard hook
            _settings.RequireAlt = _keyBindingCapture.KeyboardHook.IsAltDown;
            _settings.RequireShift = _keyBindingCapture.KeyboardHook.IsShiftDown;
            _settings.RequireCtrl = _keyBindingCapture.KeyboardHook.IsCtrlDown;

            _settings.KeyType = "mouse";
            _settings.KeyCode = code;
            _settings.KeyName = name;
            KeyLabel.Text = _settings.FullKeyName;
            App.SettingsService.Save();

            StopKeyBinding();
        });
    }

    private void OnBindingKeyPressed(int code, string name)
    {
        // Skip if it's a modifier key alone - wait for the actual key
        if (KeyboardHook.IsModifierKey(code))
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            // Escape cancels binding
            if (code == 0x1B)
            {
                StopKeyBinding();
                return;
            }

            // Backspace clears the key (sets to None)
            if (code == 0x08)
            {
                if (_settings != null)
                {
                    _settings.KeyType = "keyboard";
                    _settings.KeyCode = 0;
                    _settings.KeyName = "Yok";
                    _settings.RequireAlt = false;
                    _settings.RequireShift = false;
                    _settings.RequireCtrl = false;
                    _settings.Enabled = false;
                    KeyLabel.Text = "Yok";
                    EnabledCheckBox.IsOn = false;
                    App.SettingsService.Save();
                }
                StopKeyBinding();
                return;
            }

            if (_settings == null || _keyBindingCapture.KeyboardHook == null) return;

            // Capture current modifier states
            _settings.RequireAlt = _keyBindingCapture.KeyboardHook.IsAltDown;
            _settings.RequireShift = _keyBindingCapture.KeyboardHook.IsShiftDown;
            _settings.RequireCtrl = _keyBindingCapture.KeyboardHook.IsCtrlDown;

            _settings.KeyType = "keyboard";
            _settings.KeyCode = code;
            _settings.KeyName = name;
            KeyLabel.Text = _settings.FullKeyName;
            App.SettingsService.Save();

            StopKeyBinding();
        });
    }

    private void BaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || _settings == null) return;

        _settings.BaseText = BaseTextBox.Text;
        App.SettingsService.Save();
    }

    private void MinCharsBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isLoading || _settings == null) return;
        if (double.IsNaN(args.NewValue)) return;

        _settings.MinRandomChars = (int)args.NewValue;

        // Ensure min <= max
        if (_settings.MinRandomChars > _settings.MaxRandomChars)
        {
            _settings.MaxRandomChars = _settings.MinRandomChars;
            MaxCharsBox.Value = _settings.MaxRandomChars;
        }

        App.SettingsService.Save();
    }

    private void MaxCharsBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isLoading || _settings == null) return;
        if (double.IsNaN(args.NewValue)) return;

        _settings.MaxRandomChars = (int)args.NewValue;

        // Ensure max >= min
        if (_settings.MaxRandomChars < _settings.MinRandomChars)
        {
            _settings.MinRandomChars = _settings.MaxRandomChars;
            MinCharsBox.Value = _settings.MinRandomChars;
        }

        App.SettingsService.Save();
    }

    private void JunkCharsBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || _settings == null) return;

        _settings.JunkCharacters = JunkCharsBox.Text;
        App.SettingsService.Save();
    }
}
