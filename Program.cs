using System.Reflection;

namespace ScreenRotate;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var mutex = new Mutex(true, @"Local\ScreenRotate.SettingsWindow", out bool firstInstance);
        if (!firstInstance)
        {
            MessageBox.Show("Screen Rotate is already running. Open it from the system tray.",
                "Screen Rotate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenRotate.ScreenRotate.ico")!;
        using var icon = new Icon(iconStream);
        string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenRotate", "settings.json");
        using var window = new MainForm(icon, settingsPath);
        Application.Run(window);
    }
}
