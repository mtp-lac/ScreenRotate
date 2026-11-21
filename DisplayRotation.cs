using System.Runtime.InteropServices;

namespace ScreenRotate;

internal enum Orientation : uint
{
    Default = 0,
    Left = 1,
    UpsideDown = 2,
    Right = 3
}

internal static class DisplayRotation
{
    private const int CurrentSettings = -1;
    private const uint DisplayOrientation = 0x80;
    private const uint PelsWidth = 0x80000;
    private const uint PelsHeight = 0x100000;
    private const uint Test = 2;
    private const uint UpdateRegistry = 1;

    public static string? Rotate(string? deviceName, Orientation orientation)
    {
        if (deviceName is not null && !Screen.AllScreens.Any(screen => screen.DeviceName == deviceName))
            return "The selected display is disconnected. Choose a connected display in Settings.";

        var mode = new DisplayMode { Size = (ushort)Marshal.SizeOf<DisplayMode>() };
        if (!EnumDisplaySettings(deviceName, CurrentSettings, ref mode))
            return "Windows could not read the display settings.";

        if (mode.Orientation == orientation)
            return null;

        // A quarter turn exchanges the display's horizontal and vertical dimensions.
        if ((((uint)mode.Orientation ^ (uint)orientation) & 1) != 0)
            (mode.Width, mode.Height) = (mode.Height, mode.Width);

        mode.Orientation = orientation;
        mode.Fields |= DisplayOrientation | PelsWidth | PelsHeight;
        mode.DriverExtra = 0;
        int result = ChangeDisplaySettingsEx(deviceName, ref mode, 0, Test, 0);
        if (result == 0)
            result = ChangeDisplaySettingsEx(deviceName, ref mode, 0, UpdateRegistry, 0);

        return result switch
        {
            0 => null,
            1 => "Windows says this display mode requires a restart.",
            -2 or -6 => "The display driver does not support this rotation in the current display mode.",
            -3 => "Windows could not save the display rotation.",
            _ => $"Windows could not rotate the display (error {result})."
        };
    }

    // Offsets of the display fields in the Unicode DEVMODE structure from wingdi.h.
    [StructLayout(LayoutKind.Explicit, Size = 220)]
    private struct DisplayMode
    {
        [FieldOffset(68)] public ushort Size;
        [FieldOffset(70)] public ushort DriverExtra;
        [FieldOffset(72)] public uint Fields;
        [FieldOffset(84)] public Orientation Orientation;
        [FieldOffset(172)] public uint Width;
        [FieldOffset(176)] public uint Height;
    }

    [DllImport("user32.dll", EntryPoint = "EnumDisplaySettingsW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNumber, ref DisplayMode mode);

    [DllImport("user32.dll", EntryPoint = "ChangeDisplaySettingsExW", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, ref DisplayMode mode, nint window,
        uint flags, nint parameter);
}
