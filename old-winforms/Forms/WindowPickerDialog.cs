using DualAutoClicker.Native;

namespace DualAutoClicker.Forms;

/// <summary>
/// Dialog for selecting target windows
/// </summary>
public class WindowPickerDialog : Form
{
    private readonly CheckBox _allAppsCheckBox;
    private readonly CheckedListBox _windowListBox;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    public bool AllApps => _allAppsCheckBox.Checked;
    public List<string> SelectedProcesses { get; } = new();

    public WindowPickerDialog()
    {
        // Form settings
        this.Text = "Uygulamalar";
        this.Size = new Size(450, 400);
        this.MinimumSize = new Size(350, 300);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(25, 25, 30);
        this.ForeColor = Color.White;
        this.ShowInTaskbar = false;

        // All apps checkbox
        _allAppsCheckBox = new CheckBox
        {
            Text = "Tüm uygulamalarda aktif et",
            Location = new Point(20, 15),
            Size = new Size(400, 30),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Checked = true
        };
        _allAppsCheckBox.CheckedChanged += AllAppsCheckBox_CheckedChanged;
        this.Controls.Add(_allAppsCheckBox);

        // Window list
        _windowListBox = new CheckedListBox
        {
            Location = new Point(20, 55),
            Size = new Size(395, 250),
            BackColor = Color.FromArgb(40, 40, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.FixedSingle,
            CheckOnClick = true,
            Enabled = false
        };
        this.Controls.Add(_windowListBox);

        // OK Button
        _okButton = new Button
        {
            Text = "Tamam",
            Location = new Point(220, 315),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(0, 150, 136),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK
        };
        _okButton.FlatAppearance.BorderSize = 0;
        this.Controls.Add(_okButton);

        // Cancel Button
        _cancelButton = new Button
        {
            Text = "İptal",
            Location = new Point(320, 315),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        _cancelButton.FlatAppearance.BorderSize = 0;
        this.Controls.Add(_cancelButton);

        this.AcceptButton = _okButton;
        this.CancelButton = _cancelButton;

        // Load windows
        LoadWindows();
    }

    private void LoadWindows()
    {
        _windowListBox.Items.Clear();
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
            _windowListBox.Items.Add(displayText);
        }
    }

    private void AllAppsCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        _windowListBox.Enabled = !_allAppsCheckBox.Checked;

        if (_allAppsCheckBox.Checked)
        {
            for (int i = 0; i < _windowListBox.Items.Count; i++)
            {
                _windowListBox.SetItemChecked(i, false);
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && !AllApps)
        {
            SelectedProcesses.Clear();
            foreach (var item in _windowListBox.CheckedItems)
            {
                var text = item.ToString() ?? "";
                var processName = text.Split(" - ")[0].Trim();
                if (!string.IsNullOrEmpty(processName))
                {
                    SelectedProcesses.Add(processName);
                }
            }
        }
        base.OnFormClosing(e);
    }
}
