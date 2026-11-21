using System.Reflection;

namespace ScreenRotate;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var mutex = new Mutex(true, @"Local\ScreenRotate.SettingsWindow", out bool firstInstance);
        if (!firstInstance)
        {
            if (!args.Contains(StartupShortcut.StartupArgument, StringComparer.OrdinalIgnoreCase))
                MessageBox.Show("Screen Rotate is already running. Open it from the system tray.",
                    "Screen Rotate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenRotate.ScreenRotate.ico")!;
        using var icon = new Icon(iconStream);
        string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenRotate", "settings.json");
        var startupShortcut = new StartupShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
        using var window = new MainForm(icon, settingsPath, startupShortcut);
        RunWindow(window);
    }

    internal static void RunWindow(MainForm window)
    {
        // Running with the form as MainForm would make WinForms show it automatically.
        using var context = new ApplicationContext();
        window.FormClosed += (_, _) => context.ExitThread();
        if (window.StartMinimized)
            _ = window.Handle; // Create it hidden so Close still raises FormClosed before the first Show.
        else
            window.Show();
        Application.Run(context);
    }
}
