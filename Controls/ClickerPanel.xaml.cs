using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using DualAutoClicker.Models;

namespace DualAutoClicker.Controls;

public sealed partial class ClickerPanel : UserControl
{
    private SingleClickerSettings? _settings;
    private bool _isLoading;
    private bool _isLeftClick = true;
    private bool _isNumberBoxInputFocused;
    private readonly KeyBindingCapture _keyBindingCapture = new();
    private TextBox? _cpsInputBox;
    private TextBox? _randomInputBox;

    // Dependency Properties
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ClickerPanel),
            new PropertyMetadata("Tık", OnTitleChanged));

    public static readonly DependencyProperty IsLeftClickProperty =
        DependencyProperty.Register(nameof(IsLeftClick), typeof(bool), typeof(ClickerPanel),
            new PropertyMetadata(true, OnIsLeftClickChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
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
        ApplyHeader();
        ConfigureNumberBoxInput(CpsNumberBox, ref _cpsInputBox);
        ConfigureNumberBoxInput(RandomNumberBox, ref _randomInputBox);
        UpdateStatusIndicator();
    }

    private void ClickerPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        StopKeyBinding();
        DetachNumberBoxInput(ref _cpsInputBox);
        DetachNumberBoxInput(ref _randomInputBox);
    }

    private void ConfigureNumberBoxInput(NumberBox numberBox, ref TextBox? inputBox)
    {
        numberBox.ApplyTemplate();
        inputBox = FindTextBox(numberBox);
        if (inputBox is null) return;

        inputBox.TextAlignment = TextAlignment.Center;
        inputBox.GotFocus += NumberBoxInput_GotFocus;
        inputBox.PointerPressed += NumberBoxInput_PointerPressed;
        inputBox.PointerReleased += NumberBoxInput_PointerReleased;
        inputBox.LostFocus += NumberBoxInput_LostFocus;
    }

    private static TextBox? FindTextBox(DependencyObject parent)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int childIndex = 0; childIndex < childCount; childIndex++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, childIndex);
            if (child is TextBox textBox) return textBox;

            TextBox? descendant = FindTextBox(child);
            if (descendant is not null) return descendant;
        }

        return null;
    }

    private void DetachNumberBoxInput(ref TextBox? inputBox)
    {
        if (inputBox is null) return;

        inputBox.GotFocus -= NumberBoxInput_GotFocus;
        inputBox.PointerPressed -= NumberBoxInput_PointerPressed;
        inputBox.PointerReleased -= NumberBoxInput_PointerReleased;
        inputBox.LostFocus -= NumberBoxInput_LostFocus;
        inputBox = null;
    }

    private void NumberBoxInput_GotFocus(object sender, RoutedEventArgs e)
    {
        _isNumberBoxInputFocused = true;
        ScheduleCaretToEnd(sender as TextBox);
    }

    private void NumberBoxInput_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ScheduleCaretToEnd(sender as TextBox);
    }

    private void NumberBoxInput_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ScheduleCaretToEnd(sender as TextBox);
    }

    private void ScheduleCaretToEnd(TextBox? textBox)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => MoveCaretToEnd(textBox));
    }

    private void NumberBoxInput_LostFocus(object sender, RoutedEventArgs e)
    {
        _isNumberBoxInputFocused = false;

        if (sender is TextBox textBox)
        {
            textBox.SelectionStart = textBox.Text.Length;
            textBox.SelectionLength = 0;
        }
    }

    private void CardRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_isNumberBoxInputFocused || HasFocusableControlAncestor(e.OriginalSource as DependencyObject)) return;

        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => CardRoot.Focus(FocusState.Pointer));
    }

    private void CardRoot_GotFocus(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, CardRoot))
        {
            CardRoot.BorderBrush = ThemeBrush("AccentBrush");
        }
    }

    private void CardRoot_LostFocus(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, CardRoot))
        {
            CardRoot.BorderBrush = ThemeBrush("BorderBrush");
        }
    }

    private bool HasFocusableControlAncestor(DependencyObject? element)
    {
        while (element is not null && element != CardRoot)
        {
            if (element is Control control && control.IsEnabled && control.IsTabStop) return true;
            element = VisualTreeHelper.GetParent(element);
        }

        return false;
    }

    private static void MoveCaretToEnd(TextBox? textBox)
    {
        if (textBox is null) return;

        textBox.SelectionStart = textBox.Text.Length;
        textBox.SelectionLength = 0;
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClickerPanel panel)
        {
            panel.ApplyHeader();
        }
    }

    private static void OnIsLeftClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClickerPanel panel)
        {
            panel._isLeftClick = (bool)e.NewValue;
            panel.ApplyHeader();
        }
    }

    private void ApplyHeader()
    {
        if (TitleText is null) return;

        var title = string.IsNullOrWhiteSpace(Title) ? "Tık" : Title;
        TitleText.Text = title;
        HeaderBadgeText.Text = _isLeftClick ? "SOL" : "SAĞ";
        AutomationProperties.SetName(EnabledCheckBox, $"{title} etkinleştir");
    }

    public void LoadSettings(SingleClickerSettings settings)
    {
        _settings = settings;

        _isLoading = true;
        try
        {
            EnabledCheckBox.IsOn = settings.Enabled;
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
        }
        finally
        {
            _isLoading = false;
        }

        UpdateStatusIndicator();
    }

    public void UpdateClickingState(bool isClicking)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (isClicking)
            {
                StatusIndicator.Fill = ThemeBrush("SuccessBrush");
                StatusText.Foreground = ThemeBrush("SuccessBrush");
                StatusText.Text = "Çalışıyor";
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
            StatusIndicator.Fill = ThemeBrush("AccentTextBrush");
            StatusText.Foreground = ThemeBrush("AccentTextBrush");
            StatusText.Text = "Hazır";
        }
        else
        {
            StatusIndicator.Fill = ThemeBrush("TextMutedBrush");
            StatusText.Foreground = ThemeBrush("TextMutedBrush");
            StatusText.Text = "Kapalı";
        }
    }

    private void StartPulseAnimation()
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.45,
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
        if (_isLoading || _settings == null) return;

        _settings.Enabled = EnabledCheckBox.IsOn;
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
        ChangeKeyButton.Content = "Bekleniyor…";
        CaptureHint.Visibility = Visibility.Visible;
        _keyBindingCapture.Start(OnBindingMousePressed, OnBindingKeyPressed);
    }

    private void StopKeyBinding()
    {
        _keyBindingCapture.Stop();

        DispatcherQueue.TryEnqueue(() =>
        {
            ChangeKeyButton.Content = "Değiştir";
            CaptureHint.Visibility = Visibility.Collapsed;
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
        if (_isLoading || _settings == null) return;

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
        if (_isLoading || _settings == null) return;
        if (double.IsNaN(args.NewValue)) return;

        _settings.Cps = (int)args.NewValue;
        App.SettingsService.Save();
    }

    private void RandomNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isLoading || _settings == null) return;
        if (double.IsNaN(args.NewValue)) return;

        _settings.RandomPercent = (int)args.NewValue;
        App.SettingsService.Save();
    }

    private static Brush ThemeBrush(string key) => (Brush)Application.Current.Resources[key];
}
