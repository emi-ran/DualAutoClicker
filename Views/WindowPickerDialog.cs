using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DualAutoClicker.Native;

namespace DualAutoClicker.Views;

public sealed class WindowPickerDialog : ContentDialog
{
    private readonly CheckBox _allAppsCheckBox;
    private readonly ListView _windowListView;
    private readonly List<WindowInfo> _windows = new();

    public bool AllApps => _allAppsCheckBox.IsChecked == true;
    public List<string> SelectedProcesses { get; } = new();

    public WindowPickerDialog()
    {
        this.Title = "Uygulamalar";
        this.PrimaryButtonText = "Tamam";
        this.CloseButtonText = "İptal";
        this.DefaultButton = ContentDialogButton.Primary;

        // Build content
        var mainPanel = new StackPanel { Spacing = 16, MinWidth = 400 };

        // All apps checkbox
        _allAppsCheckBox = new CheckBox
        {
            Content = "Tüm uygulamalarda aktif et",
            IsChecked = true,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };
        _allAppsCheckBox.Checked += AllAppsCheckBox_Changed;
        _allAppsCheckBox.Unchecked += AllAppsCheckBox_Changed;
        mainPanel.Children.Add(_allAppsCheckBox);

        // Window list
        _windowListView = new ListView
        {
            Height = 300,
            SelectionMode = ListViewSelectionMode.Multiple,
            IsEnabled = false
        };
        _windowListView.ItemTemplate = CreateItemTemplate();
        mainPanel.Children.Add(_windowListView);

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
                <TextBlock Text='{Binding DisplayName}' TextTrimming='CharacterEllipsis'/>
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
    }

    private void AllAppsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _windowListView.IsEnabled = !AllApps;

        if (AllApps)
        {
            _windowListView.SelectedItems.Clear();
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

    private class WindowInfo
    {
        public string ProcessName { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
