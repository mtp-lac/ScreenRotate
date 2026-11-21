using System.Reflection;
using System.Runtime.InteropServices;

namespace ScreenRotate;

internal sealed class StartupShortcut(string? startupFolder)
{
    public const string StartupArgument = "--startup";
    private readonly string? shortcutPath = string.IsNullOrEmpty(startupFolder)
        ? null : Path.Combine(startupFolder, "ScreenRotate.exe.lnk");

    public bool IsEnabled => File.Exists(shortcutPath);

    public void SetEnabled(bool enabled)
    {
        if (shortcutPath is null)
            throw new IOException("Windows did not provide a Startup folder for this user.");
        if (!enabled)
        {
            File.Delete(shortcutPath);
            return;
        }

        string executable = Environment.ProcessPath ?? throw new IOException("The application executable could not be located.");
        string arguments = StartupArgument;
        string workingDirectory = AppContext.BaseDirectory;
        if (string.Equals(Path.GetFileName(executable), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            string? assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
            string? assemblyPath = assemblyName is null ? null : Path.Combine(workingDirectory, assemblyName + ".dll");
            if (!File.Exists(assemblyPath))
                throw new IOException("The application assembly could not be located.");

            arguments = $"\"{assemblyPath}\" {arguments}";
        }

        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new IOException("The Windows shortcut service is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic link = shell.CreateShortcut(shortcutPath);
            try
            {
                link.TargetPath = executable;
                link.Arguments = arguments;
                link.WorkingDirectory = workingDirectory;
                link.Description = "Start Screen Rotate when you sign in to Windows";
                link.Save();
                if (!IsEnabled)
                    throw new IOException("Windows did not create the Startup shortcut.");
            }
            finally
            {
                Marshal.ReleaseComObject(link);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(shell);
        }
    }
}
