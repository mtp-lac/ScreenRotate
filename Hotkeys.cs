using System.Runtime.InteropServices;

namespace ScreenRotate;

internal sealed class Hotkeys : NativeWindow, IDisposable
{
    private const int HotkeyMessage = 0x0312;
    private const uint NoRepeat = 0x4000;
    private readonly Dictionary<Shortcut, int> registrations = [];
    private Dictionary<int, Orientation> actions = [];
    private int lastId;

    public event Action<Orientation>? Pressed;

    public Hotkeys() => CreateHandle(new CreateParams { Parent = new IntPtr(-3) }); // HWND_MESSAGE

    public bool TrySet(IReadOnlyDictionary<Shortcut, Orientation> desired, Action save, out Shortcut unavailable)
    {
        var added = new List<Shortcut>();
        bool committed = false;
        try
        {
            foreach (Shortcut shortcut in desired.Keys)
            {
                if (registrations.ContainsKey(shortcut))
                    continue;

                if (!Register(shortcut))
                {
                    unavailable = shortcut;
                    return false;
                }

                added.Add(shortcut);
            }

            save();
            committed = true;
        }
        finally
        {
            if (!committed)
                foreach (Shortcut shortcut in added)
                    Unregister(shortcut);
        }

        foreach (Shortcut shortcut in registrations.Keys.Except(desired.Keys).ToArray())
            Unregister(shortcut);

        actions = desired.ToDictionary(item => registrations[item.Key], item => item.Value);
        unavailable = default;
        return true;
    }

    public bool TryAdd(Shortcut shortcut, Orientation action)
    {
        if (!Register(shortcut))
            return false;

        actions.Add(registrations[shortcut], action);
        return true;
    }

    private bool Register(Shortcut shortcut)
    {
        if (registrations.ContainsKey(shortcut))
            return false;

        // Keep IDs in the application range and avoid reusing the IDs of active registrations.
        do
        {
            lastId = lastId % 0xBFFF + 1;
        } while (registrations.ContainsValue(lastId));

        if (!RegisterHotKey(Handle, lastId, (uint)shortcut.Modifiers | NoRepeat, (uint)shortcut.Key))
            return false;

        registrations.Add(shortcut, lastId);
        return true;
    }

    private void Unregister(Shortcut shortcut)
    {
        if (registrations.Remove(shortcut, out int id))
            UnregisterHotKey(Handle, id);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == HotkeyMessage && actions.TryGetValue((int)message.WParam, out var action))
        {
            Pressed?.Invoke(action);
            return;
        }

        base.WndProc(ref message);
    }

    public void Dispose()
    {
        foreach (Shortcut shortcut in registrations.Keys.ToArray())
            Unregister(shortcut);
        DestroyHandle();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint window, int id);
}
