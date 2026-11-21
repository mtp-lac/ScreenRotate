# Screen Rotate

A Windows tray utility for rotating a display with global keyboard shortcuts. Choose a display and customize or disable each shortcut in the settings window, then select **Apply**. The primary display is selected by default.

| Orientation | Default shortcut |
| --- | --- |
| Normal | **Ctrl + Alt + Shift + Up** |
| Rotate left 90° | **Ctrl + Alt + Shift + Left** |
| Upside down 180° | **Ctrl + Alt + Shift + Down** |
| Rotate right 90° | **Ctrl + Alt + Shift + Right** |

Minimizing or closing the window hides it in the system tray and keeps shortcuts active. Double-click the tray icon to reopen settings. Right-click it to rotate the selected display or choose **Exit**. If the icon is hidden, check the tray overflow arrow.

Settings are saved in `%LOCALAPPDATA%\ScreenRotate\settings.json`. Rotation depends on the display driver and current display mode.

## Build and run

Use the .NET 10 SDK on Windows. A framework-dependent build requires the .NET 10 Desktop Runtime on the destination PC.

```powershell
dotnet run --project .\ScreenRotate.csproj
```

To publish a standalone executable for 64-bit Windows, run:

```powershell
dotnet publish .\ScreenRotate.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o .\bin\portable
```

If your build folder has a Low integrity label, place the published executable in a regular user folder before launching so it can register the tray icon.
