using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using DualAutoClicker.Models;
using DualAutoClicker.Services;
using Windows.UI;

namespace DualAutoClicker.Controls;

public sealed partial class ClickerPanel : UserControl
{
    private SingleClickerSettings? _settings;
    private bool _isLeftClick = true;
    private readonly KeyBindingCapture _keyBindingCapture = new();

    // Dependency Properties
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ClickerPanel), 
            new PropertyMetadata("CLICK", OnTitleChanged));

    public static readonly DependencyProperty AccentColorProperty =
        DependencyProperty.Register(nameof(AccentColor), typeof(Color), typeof(ClickerPanel),
            new PropertyMetadata(Colors.Cyan, OnAccentColorChanged));

    public static readonly DependencyProperty SecondaryColorProperty =
        DependencyProperty.Register(nameof(SecondaryColor), typeof(Color), typeof(ClickerPanel),
            new PropertyMetadata(Colors.Purple, OnSecondaryColorChanged));

    public static readonly DependencyProperty IsLeftClickProperty =
        DependencyProperty.Register(nameof(IsLeftClick), typeof(bool), typeof(ClickerPanel),
            new PropertyMetadata(true, OnIsLeftClickChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public Color SecondaryColor
    {
        get => (Color)GetValue(SecondaryColorProperty);
        set => SetValue(SecondaryColorProperty, value);
    }

    public bool IsLeftClick
    {
        get => (bool)GetValue(IsLeftClickProperty);
        set => SetValue(IsLeftClickProperty, value);
    }

    public ClickerPanel()
    {
        this.InitializeComponent();
        this.Loaded += ClickerPanel_Loaded;
        this.Unloaded += ClickerPanel_Unloaded;
    }

    private void ClickerPanel_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateColors();
    }

    private void ClickerPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        StopKeyBinding();
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClickerPanel panel)
        {
            panel.EnabledCheckBox.Content = e.NewValue?.ToString() ?? "CLICK";
        }
    }

    private static void OnAccentColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClickerPanel panel)
        {
            panel.UpdateColors();
        }
    }

    private static void OnSecondaryColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClickerPanel panel)
        {
            panel.UpdateColors();
        }
    }

    private static void OnIsLeftClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClickerPanel panel)
        {
            panel._isLeftClick = (bool)e.NewValue;
        }
    }

    private void UpdateColors()
    {
        // Update border gradient
        var gradientBrush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 0)
        };
        gradientBrush.GradientStops.Add(new GradientStop { Color = AccentColor, Offset = 0 });
        gradientBrush.GradientStops.Add(new GradientStop { Color = SecondaryColor, Offset = 1 });
        CardBorder.BorderBrush = gradientBrush;

        // Update accent elements
        KeyLabel.Foreground = new SolidColorBrush(AccentColor);
        CpsLabel.Foreground = new SolidColorBrush(AccentColor);
        RandomLabel.Foreground = new SolidColorBrush(SecondaryColor);

        // Update button color
        ChangeKeyButton.Background = new SolidColorBrush(AccentColor);
    }

    public void LoadSettings(SingleClickerSettings settings)
    {
        _settings = settings;

        EnabledCheckBox.IsChecked = settings.Enabled;
        KeyLabel.Text = settings.KeyName;
        HoldRadio.IsChecked = settings.Mode == ActivationMode.Hold;
        ToggleRadio.IsChecked = settings.Mode == ActivationMode.Toggle;

        if (HoldRadio.IsChecked != true && ToggleRadio.IsChecked != true)
        {
            HoldRadio.IsChecked = true;
            settings.Mode = ActivationMode.Hold;
            App.SettingsService.Save();
        }

        CpsNumberBox.Value = Math.Clamp(settings.Cps, 1, 100);
        RandomNumberBox.Value = Math.Clamp(settings.RandomPercent, 0, 30);

        UpdateStatusIndicator();
    }

    public void UpdateClickingState(bool isClicking)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (isClicking)
            {
                StatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 136)); // Green
                StartPulseAnimation();
            }
            else
            {
                StopPulseAnimation();
                UpdateStatusIndicator();
            }
        });
    }

    private void UpdateStatusIndicator()
    {
        if (_settings?.Enabled == true)
        {
            StatusIndicator.Fill = new SolidColorBrush(AccentColor);
        }
        else
        {
            StatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 90, 90, 110)); // Muted
        }
    }

    private void StartPulseAnimation()
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.5,
            Duration = new Duration(TimeSpan.FromMilliseconds(500)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTarget(animation, StatusIndicator);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void StopPulseAnimation()
    {
        StatusIndicator.Opacity = 1.0;
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_settings == null) return;

        _settings.Enabled = EnabledCheckBox.IsChecked == true;
        App.SettingsService.Save();
        UpdateStatusIndicator();
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
        ChangeKeyButton.Content = "...";
        ChangeKeyButton.Background = new SolidColorBrush(Color.FromArgb(255, 180, 130, 0));
        _keyBindingCapture.Start(OnBindingMousePressed, OnBindingKeyPressed);
    }

    private void StopKeyBinding()
    {
        _keyBindingCapture.Stop();

        DispatcherQueue.TryEnqueue(() =>
        {
            ChangeKeyButton.Content = "Değiştir";
            ChangeKeyButton.Background = new SolidColorBrush(AccentColor);
        });
    }

    private void OnBindingMousePressed(int code, string name)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_settings == null) return;

            _settings.KeyType = "mouse";
            _settings.KeyCode = code;
            _settings.KeyName = name;
            KeyLabel.Text = name;
            App.SettingsService.Save();

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

            if (_settings == null) return;

            _settings.KeyType = "keyboard";
            _settings.KeyCode = code;
            _settings.KeyName = name;
            KeyLabel.Text = name;
            App.SettingsService.Save();

            StopKeyBinding();
        });
    }

    private void ModeRadio_Changed(object sender, RoutedEventArgs e)
    {
        if (_settings == null) return;

        if (sender is RadioButton button)
        {
            if (button == HoldRadio)
            {
                _settings.Mode = ActivationMode.Hold;
            }
            else if (button == ToggleRadio)
            {
                _settings.Mode = ActivationMode.Toggle;
            }
        }

        App.SettingsService.Save();
    }

    private void CpsNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_settings == null) return;
        if (double.IsNaN(args.NewValue)) return;

        _settings.Cps = (int)args.NewValue;
        App.SettingsService.Save();
    }

    private void RandomNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_settings == null) return;
        if (double.IsNaN(args.NewValue)) return;

        _settings.RandomPercent = (int)args.NewValue;
        App.SettingsService.Save();
    }
}
