using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DualAutoClicker.Native;


namespace DualAutoClicker.Views;

public sealed class WindowPickerDialog : ContentDialog
{
    private readonly CheckBox _allAppsCheckBox;
    private readonly ListView _windowListView;
    private readonly List<WindowInfo> _windows = new();
    private readonly HashSet<string> _initialSelections = new(StringComparer.OrdinalIgnoreCase);

    public bool AllApps => _allAppsCheckBox.IsChecked == true;
    public List<string> SelectedProcesses { get; } = new();

    public WindowPickerDialog(IEnumerable<string>? selectedProcesses = null, bool allApps = true)
    {
        this.Title = "Hedef uygulamalar";
        this.PrimaryButtonText = "Kaydet";
        this.CloseButtonText = "İptal";
        this.DefaultButton = ContentDialogButton.Primary;
        this.Background = ThemeBrush("BgElevatedBrush");
        this.BorderBrush = ThemeBrush("BorderBrush");
        this.BorderThickness = new Thickness(1);
        this.CornerRadius = new CornerRadius(12);
        this.PrimaryButtonStyle = ThemeStyle("AccentButtonStyle");
        this.CloseButtonStyle = ThemeStyle("SecondaryButtonStyle");

        if (selectedProcesses != null)
        {
            foreach (var process in selectedProcesses)
            {
                if (!string.IsNullOrWhiteSpace(process))
                {
                    _initialSelections.Add(process.Trim());
                }
            }
        }

        // Build content
        var mainPanel = new StackPanel { Spacing = 12, MinWidth = 440 };

        var description = new TextBlock
        {
            Text = "Yalnızca seçtiğiniz uygulamalar ön plandayken otomasyon çalışır.",
            Style = ThemeStyle("CaptionTextStyle")
        };
        mainPanel.Children.Add(description);

        // All apps checkbox
        _allAppsCheckBox = new CheckBox
        {
            Content = "Tüm uygulamalarda çalıştır",
            IsChecked = allApps,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = ThemeBrush("TextPrimaryBrush")
        };
        AutomationProperties.SetName(_allAppsCheckBox, "Tüm uygulamalarda çalıştır");

        _allAppsCheckBox.Checked += AllAppsCheckBox_Changed;
        _allAppsCheckBox.Unchecked += AllAppsCheckBox_Changed;
        mainPanel.Children.Add(_allAppsCheckBox);

        var listLabel = new TextBlock
        {
            Text = "AÇIK UYGULAMALAR",
            Style = ThemeStyle("SectionLabelTextStyle"),
            Margin = new Thickness(0, 4, 0, 0)
        };
        mainPanel.Children.Add(listLabel);

        // Window list
        _windowListView = new ListView
        {
            Height = 300,
            SelectionMode = ListViewSelectionMode.Multiple,
            IsEnabled = !allApps,
            Background = ThemeBrush("BgInputBrush"),
            BorderBrush = ThemeBrush("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(4)
        };
        AutomationProperties.SetName(_windowListView, "Açık uygulamalar");
        _windowListView.ItemTemplate = CreateItemTemplate();
        mainPanel.Children.Add(_windowListView);

        AllAppsCheckBox_Changed(_allAppsCheckBox, new RoutedEventArgs());

        this.Content = mainPanel;

        // Wire up primary button click
        this.PrimaryButtonClick += OnPrimaryButtonClicked;

        // Load windows
        LoadWindows();
    }

    private DataTemplate CreateItemTemplate()
    {
        // Simple text template
        var template = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <TextBlock Text='{Binding DisplayName}' TextTrimming='CharacterEllipsis' Margin='4,6' />
            </DataTemplate>");
        return template;
    }

    private void LoadWindows()
    {
        _windows.Clear();
        var windows = WindowEnumerator.GetOpenWindows();

        // Group by process and show unique entries
        var uniqueProcesses = windows
            .GroupBy(w => w.ProcessName)
            .Select(g => g.First())
            .ToList();

        foreach (var window in uniqueProcesses)
        {
            string displayText = string.IsNullOrEmpty(window.Title)
                ? window.ProcessName
                : $"{window.ProcessName} - {(window.Title.Length > 50 ? window.Title[..47] + "..." : window.Title)}";

            _windows.Add(new WindowInfo
            {
                ProcessName = window.ProcessName,
                DisplayName = displayText
            });
        }

        _windowListView.ItemsSource = _windows;

        if (_initialSelections.Count > 0)
        {
            var selections = _windows
                .Where(window => _initialSelections.Contains(window.ProcessName))
                .ToList();
            foreach (var window in selections)
            {
                _windowListView.SelectedItems.Add(window);
            }
        }
    }


    private void AllAppsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _windowListView.IsEnabled = !AllApps;

        if (AllApps)
        {
            _windowListView.SelectedItems.Clear();
        }
        else if (_windowListView.SelectedItems.Count == 0 && _initialSelections.Count > 0)
        {
            var selections = _windows
                .Where(window => _initialSelections.Contains(window.ProcessName))
                .ToList();
            foreach (var window in selections)
            {
                _windowListView.SelectedItems.Add(window);
            }
        }
    }


    private void OnPrimaryButtonClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!AllApps)
        {
            SelectedProcesses.Clear();
            foreach (var item in _windowListView.SelectedItems)
            {
                if (item is WindowInfo info)
                {
                    SelectedProcesses.Add(info.ProcessName);
                }
            }
        }
    }

    private static Brush ThemeBrush(string key) => (Brush)Application.Current.Resources[key];

    private static Style ThemeStyle(string key) => (Style)Application.Current.Resources[key];

    private class WindowInfo
    {
        public string ProcessName { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
