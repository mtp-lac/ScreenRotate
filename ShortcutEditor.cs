namespace ScreenRotate;

internal sealed class ShortcutEditor : FlowLayoutPanel
{
    private readonly ComboBox key = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 112, MaxDropDownItems = 14 };
    private readonly (CheckBox Box, Modifiers Modifier)[] modifiers;

    public ShortcutEditor(string action)
    {
        WrapContents = false;
        key.AccessibleName = $"{action} key";
        modifiers =
        [
            (CreateCheckBox(action, "Ctrl"), Modifiers.Control),
            (CreateCheckBox(action, "Alt"), Modifiers.Alt),
            (CreateCheckBox(action, "Shift"), Modifiers.Shift),
            (CreateCheckBox(action, "Win"), Modifiers.Windows)
        ];

        foreach (var (box, _) in modifiers)
            Controls.Add(box);

        key.Items.Add(new KeyChoice(Keys.None));
        foreach (Keys binding in Shortcut.SupportedKeys)
            key.Items.Add(new KeyChoice(binding));

        key.SelectedIndexChanged += (_, _) =>
        {
            foreach (var (box, _) in modifiers)
                box.Enabled = SelectedKey != Keys.None;
        };
        Controls.Add(key);
        key.SelectedIndex = 0;
    }

    private Keys SelectedKey => ((KeyChoice)key.SelectedItem!).Key;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Shortcut Value
    {
        get
        {
            if (SelectedKey == Keys.None)
                return default;

            var selected = Modifiers.None;
            foreach (var (box, modifier) in modifiers)
                if (box.Checked) selected |= modifier;

            return new Shortcut(selected, SelectedKey);
        }
        set
        {
            foreach (var (box, modifier) in modifiers)
                box.Checked = value.Modifiers.HasFlag(modifier);

            key.SelectedItem = key.Items.Cast<KeyChoice>().First(choice => choice.Key == value.Key);
        }
    }

    private static CheckBox CreateCheckBox(string action, string label) => new()
    {
        Text = label,
        AccessibleName = $"{action} {label}",
        AutoSize = true,
        Margin = new Padding(2, 6, 8, 0)
    };

    private sealed record KeyChoice(Keys Key)
    {
        public override string ToString() => Key == Keys.None ? "Disabled" : Shortcut.KeyName(Key);
    }
}
