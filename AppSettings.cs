using System.Text.Json;

namespace ScreenRotate;

[Flags]
internal enum Modifiers : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

internal readonly record struct Shortcut(Modifiers Modifiers, Keys Key)
{
    public static readonly Keys[] SupportedKeys =
    [
        .. Enumerable.Range((int)Keys.A, 26).Select(value => (Keys)value),
        .. Enumerable.Range((int)Keys.D0, 10).Select(value => (Keys)value),
        Keys.Up, Keys.Left, Keys.Down, Keys.Right,
        .. Enumerable.Range((int)Keys.F1, 24).Where(value => value != (int)Keys.F12).Select(value => (Keys)value),
        Keys.Space, Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown, Keys.Insert, Keys.Delete,
        .. Enumerable.Range((int)Keys.NumPad0, 10).Select(value => (Keys)value)
    ];

    public bool IsDisabled => this == default;
    public bool IsValid => IsDisabled || (Modifiers != Modifiers.None &&
        (Modifiers & ~(Modifiers.Alt | Modifiers.Control | Modifiers.Shift | Modifiers.Windows)) == 0 &&
        SupportedKeys.Contains(Key));

    public static string KeyName(Keys key) => key switch
    {
        >= Keys.D0 and <= Keys.D9 => ((int)key - (int)Keys.D0).ToString(),
        >= Keys.NumPad0 and <= Keys.NumPad9 => $"NumPad {(int)key - (int)Keys.NumPad0}",
        Keys.PageUp => "Page Up",
        Keys.PageDown => "Page Down",
        _ => key.ToString()
    };

    public override string ToString()
    {
        if (IsDisabled)
            return "Disabled";

        var parts = new List<string>();
        if (Modifiers.HasFlag(Modifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(Modifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(Modifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(Modifiers.Windows)) parts.Add("Win");
        parts.Add(KeyName(Key));
        return string.Join(" + ", parts);
    }
}

internal sealed record AppSettings(string? Display, Shortcut Normal, Shortcut Left, Shortcut UpsideDown,
    Shortcut Right, bool StartMinimized = false)
{
    public static AppSettings Defaults { get; } = new(null,
        new(DefaultModifiers, Keys.Up), new(DefaultModifiers, Keys.Left),
        new(DefaultModifiers, Keys.Down), new(DefaultModifiers, Keys.Right));

    private const Modifiers DefaultModifiers = Modifiers.Control | Modifiers.Alt | Modifiers.Shift;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, IgnoreReadOnlyProperties = true };

    public Shortcut GetShortcut(Orientation orientation) => orientation switch
    {
        Orientation.Default => Normal,
        Orientation.Left => Left,
        Orientation.UpsideDown => UpsideDown,
        Orientation.Right => Right,
        _ => throw new ArgumentOutOfRangeException(nameof(orientation))
    };

    public static (AppSettings Settings, string? Warning) Load(string path)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions);
            if (settings is not null && Enum.GetValues<Orientation>().All(value => settings.GetShortcut(value).IsValid))
                return (settings, null);

            return (Defaults, "The settings file contains invalid shortcuts. Default settings are loaded.");
        }
        catch (FileNotFoundException)
        {
            return (Defaults, null);
        }
        catch (DirectoryNotFoundException)
        {
            return (Defaults, null);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return (Defaults, "Could not read saved settings. Default settings are loaded.");
        }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, JsonOptions));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
