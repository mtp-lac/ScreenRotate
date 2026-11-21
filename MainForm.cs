namespace ScreenRotate;

internal sealed class MainForm : Form
{
    private static readonly (Orientation Orientation, string Label)[] Directions =
    [
        (Orientation.Default, "Normal"),
        (Orientation.Left, "Rotate left (90°)"),
        (Orientation.UpsideDown, "Upside down (180°)"),
        (Orientation.Right, "Rotate right (90°)")
    ];

    private readonly string settingsPath;
    private readonly StartupShortcut startupShortcut;
    private readonly CheckBox startWithWindows = new() { Text = "Start with &Windows", AutoSize = true };
    private readonly CheckBox startMinimized = new() { Text = "Start &minimized to tray", AutoSize = true };
    private readonly ToolTip startupTip = new() { AutoPopDelay = 12000 };
    private readonly Hotkeys hotkeys = new();
    private readonly ComboBox displays = new() { DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Display" };
    private readonly Dictionary<Orientation, ShortcutEditor> editors = [];
    private readonly Dictionary<Orientation, ToolStripMenuItem> trayRotationItems = [];
    private readonly Dictionary<Orientation, Shortcut> activeShortcuts = [];
    private readonly Label status = new() { AutoSize = false, Dock = DockStyle.Fill };
    private readonly ContextMenuStrip trayMenu = new();
    private readonly NotifyIcon tray;
    private AppSettings settings;
    private bool exiting;

    public bool StartMinimized => settings.StartMinimized;

    public MainForm(Icon icon, string settingsPath, StartupShortcut startupShortcut)
    {
        this.settingsPath = settingsPath;
        this.startupShortcut = startupShortcut;
        var (loaded, warning) = AppSettings.Load(settingsPath);
        settings = loaded;

        Text = "Screen Rotate";
        Icon = icon;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(550, 420);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BuildSettings();
        LoadEditors(settings);
        startWithWindows.Checked = startupShortcut.IsEnabled;
        displays.DropDown += (_, _) => RefreshDisplays(SelectedDisplay);

        trayMenu.Items.Add("Open settings", null, (_, _) => ShowSettings());
        trayMenu.Items.Add(new ToolStripSeparator());
        foreach (var (orientation, label) in Directions)
        {
            var item = new ToolStripMenuItem(label, null, (_, _) => Rotate(orientation));
            trayRotationItems.Add(orientation, item);
            trayMenu.Items.Add(item);
        }
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, (_, _) => { exiting = true; Close(); });
        tray = new NotifyIcon { Text = Text, ContextMenuStrip = trayMenu, Icon = icon, Visible = true };
        tray.DoubleClick += (_, _) => ShowSettings();

        hotkeys.Pressed += Rotate;
        var unavailable = new List<string>();
        foreach (var (orientation, _) in Directions)
        {
            Shortcut shortcut = settings.GetShortcut(orientation);
            if (shortcut.IsDisabled)
                continue;

            if (hotkeys.TryAdd(shortcut, orientation))
                activeShortcuts.Add(orientation, shortcut);
            else
                unavailable.Add(shortcut.ToString());
        }
        UpdateTrayShortcuts();

        if (unavailable.Count != 0)
            SetStatus($"Unavailable shortcuts: {string.Join(", ", unavailable)}. Choose other keys and apply.", true);
        else if (warning is not null)
            SetStatus(warning, true);
        else
            SetStatus("Shortcuts work even when Screen Rotate is in the tray.");
    }

    private void BuildSettings()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        Controls.Add(layout);

        var title = new Label { Text = Text, AutoSize = true, Font = new Font(Font.FontFamily, 17, FontStyle.Bold) };
        layout.Controls.Add(title, 0, 0);

        var displayRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        displayRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        displayRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        displayRow.Controls.Add(new Label { Text = "&Display", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        displays.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        displayRow.Controls.Add(displays, 1, 0);
        layout.Controls.Add(displayRow, 0, 1);

        layout.Controls.Add(new Label { Text = "Global shortcuts", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        var shortcutRows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        shortcutRows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        shortcutRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(shortcutRows, 0, 3);
        foreach (var (orientation, label) in Directions)
        {
            int row = editors.Count;
            var editor = new ShortcutEditor(label) { Dock = DockStyle.Fill, Margin = Padding.Empty };
            editors.Add(orientation, editor);
            shortcutRows.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            shortcutRows.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            shortcutRows.Controls.Add(editor, 1, row);
        }

        var startupOptions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        startWithWindows.Margin = new Padding(3, 6, 18, 0);
        const string startupWarning = "The Startup shortcut remembers where Screen Rotate was when you enabled it.\n" +
            "If you move or rename the app, Windows may no longer start it at sign-in.\n" +
            "Open it from the new location, uncheck this option and click Apply, then check it and click Apply again.";
        startupTip.SetToolTip(startWithWindows, startupWarning);
        startWithWindows.AccessibleDescription = startupWarning;
        startMinimized.Margin = new Padding(3, 6, 3, 0);
        startupOptions.Controls.Add(startWithWindows);
        startupOptions.Controls.Add(startMinimized);
        layout.Controls.Add(startupOptions, 0, 4);

        status.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(status, 0, 5);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        var apply = MakeButton("&Apply", (_, _) => ApplySettings());
        buttons.Controls.Add(apply);
        buttons.Controls.Add(MakeButton("Hide to &tray", (_, _) => HideToTray()));
        buttons.Controls.Add(MakeButton("Restore de&faults", (_, _) =>
        {
            LoadEditors(AppSettings.Defaults);
            startWithWindows.Checked = false;
            SetStatus("Defaults are shown. Select Apply to save them.");
        }));
        layout.Controls.Add(buttons, 0, 6);
        AcceptButton = apply;
    }

    private static Button MakeButton(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(90, 32) };
        button.Click += click;
        return button;
    }

    private string? SelectedDisplay => (displays.SelectedItem as DisplayChoice)?.DeviceName;

    private void RefreshDisplays(string? selected)
    {
        displays.Items.Clear();
        displays.Items.Add(new DisplayChoice(null, "Primary display (automatic)"));
        foreach (Screen screen in Screen.AllScreens.OrderBy(screen => screen.DeviceName, StringComparer.Ordinal))
        {
            string name = screen.DeviceName.Replace(@"\\.\DISPLAY", "Display ", StringComparison.OrdinalIgnoreCase);
            string primary = screen.Primary ? " · primary" : "";
            displays.Items.Add(new DisplayChoice(screen.DeviceName,
                $"{name} · {screen.Bounds.Width} × {screen.Bounds.Height}{primary}"));
        }

        if (selected is not null && !displays.Items.Cast<DisplayChoice>().Any(choice => choice.DeviceName == selected))
            displays.Items.Add(new DisplayChoice(selected, $"{selected} (disconnected)"));

        displays.SelectedItem = displays.Items.Cast<DisplayChoice>().First(choice => choice.DeviceName == selected);
    }

    private void LoadEditors(AppSettings source)
    {
        RefreshDisplays(source.Display);
        startMinimized.Checked = source.StartMinimized;
        foreach (var (orientation, editor) in editors)
            editor.Value = source.GetShortcut(orientation);
    }

    private void ApplySettings()
    {
        var desired = new Dictionary<Shortcut, Orientation>();
        foreach (var (orientation, label) in Directions)
        {
            Shortcut shortcut = editors[orientation].Value;
            if (!shortcut.IsValid)
            {
                SetStatus($"Choose at least one modifier for {label}, or select Disabled.", true);
                return;
            }
            if (!shortcut.IsDisabled && !desired.TryAdd(shortcut, orientation))
            {
                SetStatus($"{shortcut} is assigned more than once. Use a different shortcut for each action.", true);
                return;
            }
        }

        var candidate = new AppSettings(SelectedDisplay, editors[Orientation.Default].Value, editors[Orientation.Left].Value,
            editors[Orientation.UpsideDown].Value, editors[Orientation.Right].Value, startMinimized.Checked);
        try
        {
            if (!hotkeys.TrySet(desired, () => candidate.Save(settingsPath), out Shortcut unavailable))
            {
                SetStatus($"{unavailable} is reserved or in use by another app. Your settings are unchanged.", true);
                return;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            SetStatus("Could not save settings. Your previous shortcuts are still active.", true);
            return;
        }

        settings = candidate;
        activeShortcuts.Clear();
        foreach (var (shortcut, orientation) in desired)
            activeShortcuts.Add(orientation, shortcut);
        UpdateTrayShortcuts();
        try
        {
            if (startupShortcut.IsEnabled != startWithWindows.Checked)
                startupShortcut.SetEnabled(startWithWindows.Checked);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
            SetStatus("Settings saved, but the Windows Startup shortcut could not be updated. Check the Startup folder and try again.", true);
            return;
        }

        SetStatus("Settings saved.");
    }

    private void UpdateTrayShortcuts()
    {
        foreach (var (orientation, item) in trayRotationItems)
            item.ShortcutKeyDisplayString = activeShortcuts.TryGetValue(orientation, out var shortcut) ? shortcut.ToString() : "";
    }

    private void Rotate(Orientation orientation)
    {
        string? error = DisplayRotation.Rotate(settings.Display, orientation);
        if (error is null)
            return;

        if (Visible)
            SetStatus(error, true);
        else
            tray.ShowBalloonTip(4000, Text, error, ToolTipIcon.Warning);
    }

    private void SetStatus(string text, bool error = false)
    {
        status.Text = text;
        status.ForeColor = error && !SystemInformation.HighContrast ? Color.Firebrick : SystemColors.ControlText;
    }

    private void HideToTray()
    {
        Hide();
        WindowState = FormWindowState.Normal;
    }

    private void ShowSettings()
    {
        RefreshDisplays(SelectedDisplay);
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
            HideToTray();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!exiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            hotkeys.Dispose();
            tray?.Dispose();
            trayMenu.Dispose();
            startupTip.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed record DisplayChoice(string? DeviceName, string Label)
    {
        public override string ToString() => Label;
    }
}
