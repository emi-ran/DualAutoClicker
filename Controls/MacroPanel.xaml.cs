using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DualAutoClicker.Models;
using DualAutoClicker.Services;
using DualAutoClicker.Native;
using Windows.UI;

namespace DualAutoClicker.Controls;

public sealed partial class MacroPanel : UserControl
{
    private KeyboardMacroSettings? _settings;
    private bool _isBinding = false;
    private MouseHook? _bindingMouseHook;
    private KeyboardHook? _bindingKeyboardHook;

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
        _settings = settingsService.Settings.KeyboardMacro;

        EnabledCheckBox.IsChecked = _settings.Enabled;
        KeyLabel.Text = _settings.FullKeyName; // Use FullKeyName to show modifiers
        BaseTextBox.Text = _settings.BaseText;
        MinCharsBox.Value = _settings.MinRandomChars;
        MaxCharsBox.Value = _settings.MaxRandomChars;
        JunkCharsBox.Text = _settings.JunkCharacters;
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_settings == null) return;

        _settings.Enabled = EnabledCheckBox.IsChecked == true;
        App.SettingsService.Save();
    }

    private void ChangeKeyButton_Click(object sender, RoutedEventArgs e)
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
        ChangeKeyButton.Content = "...";
        ChangeKeyButton.Background = new SolidColorBrush(Color.FromArgb(255, 180, 130, 0));

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
            ChangeKeyButton.Content = "Seç";
            var greenColor = (Color)Application.Current.Resources["AccentGreenColor"];
            ChangeKeyButton.Background = new SolidColorBrush(greenColor);
        });
    }

    private void OnBindingMousePressed(int code, string name)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_settings == null || _bindingKeyboardHook == null) return;

            // Capture current modifier states from keyboard hook
            _settings.RequireAlt = _bindingKeyboardHook.IsAltDown;
            _settings.RequireShift = _bindingKeyboardHook.IsShiftDown;
            _settings.RequireCtrl = _bindingKeyboardHook.IsCtrlDown;

            _settings.KeyType = "mouse";
            _settings.KeyCode = code;
            _settings.KeyName = name;
            KeyLabel.Text = _settings.FullKeyName; // Show full combo like "Alt+MB4"
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
                    EnabledCheckBox.IsChecked = false;
                    App.SettingsService.Save();
                }
                StopKeyBinding();
                return;
            }

            if (_settings == null || _bindingKeyboardHook == null) return;

            // Capture current modifier states
            _settings.RequireAlt = _bindingKeyboardHook.IsAltDown;
            _settings.RequireShift = _bindingKeyboardHook.IsShiftDown;
            _settings.RequireCtrl = _bindingKeyboardHook.IsCtrlDown;

            _settings.KeyType = "keyboard";
            _settings.KeyCode = code;
            _settings.KeyName = name;
            KeyLabel.Text = _settings.FullKeyName; // Show full combo like "Alt+Shift+3"
            App.SettingsService.Save();

            StopKeyBinding();
        });
    }

    private void BaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_settings == null) return;

        _settings.BaseText = BaseTextBox.Text;
        App.SettingsService.Save();
    }

    private void MinCharsBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_settings == null) return;
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
        if (_settings == null) return;
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
        if (_settings == null) return;

        _settings.JunkCharacters = JunkCharsBox.Text;
        App.SettingsService.Save();
    }
}
