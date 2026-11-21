# Screen Rotate

A Windows tray utility for rotating a display with global keyboard shortcuts. Choose a display and customize or disable each shortcut in the settings window, then select **Apply**. The primary display is selected by default.

| Orientation | Default shortcut |
| --- | --- |
| Normal | **Ctrl + Alt + Shift + Up** |
| Rotate left 90° | **Ctrl + Alt + Shift + Left** |
| Upside down 180° | **Ctrl + Alt + Shift + Down** |
| Rotate right 90° | **Ctrl + Alt + Shift + Right** |

Minimizing or closing the window hides it in the system tray and keeps shortcuts active. Double-click the tray icon to reopen settings. Right-click it to rotate the selected display or choose **Exit**. If the icon is hidden, check the tray overflow arrow.

Rotation depends on the display driver and current display mode.

## Settings

Screen Rotate stores settings for the current Windows user in:

```text
%LOCALAPPDATA%\ScreenRotate\settings.json
```

This normally expands to `C:\Users\<username>\AppData\Local\ScreenRotate\settings.json`. Paste `%LOCALAPPDATA%\ScreenRotate` into File Explorer's address bar to open the folder. All copies of the app run by the same Windows user share this file, regardless of where the executable is located.

### Startup options

- **Start with Windows** adds `ScreenRotate.exe.lnk` to your Windows Startup folder (`shell:startup` in File Explorer). Uncheck it and select **Apply** to remove the shortcut. An existing shortcut with that name is shown as enabled. Windows can separately disable a Startup entry in Settings or Task Manager.
- **Start minimized to tray** makes Screen Rotate launch directly in the notification area without opening Settings. This works for manual launches as well as launches at sign-in. Double-click the tray icon to open Settings.

Both options are off by default unless a Startup shortcut already exists. Select **Apply** to make changes. The Startup shortcut remembers the executable's location. If you move or rename it, open Screen Rotate from the new location, uncheck **Start with Windows** and select **Apply**, then check it again and select **Apply** to recreate the shortcut. Hover over **Start with Windows** for this reminder.

### Saving and resetting

- Select **Apply** to save the display selection, shortcuts, and startup options. Shortcut changes take effect immediately; **Start minimized to tray** takes effect on the next launch. The settings folder and file are created on the first successful Apply; running the app with defaults does not create them.
- Closing or minimizing the window keeps the app running in the tray. It does not save edits you have not applied.
- Select **Restore defaults**, then **Apply**, to reset the display selection, shortcuts, and startup options.
- To reset the JSON settings manually, choose **Exit** from the tray, delete `settings.json`, and restart the app. This does not remove the Startup folder shortcut. A missing file loads the defaults. An unreadable file, malformed JSON, or invalid shortcut loads the defaults and shows a warning.
- Duplicate shortcuts or shortcuts reserved by Windows or another app prevent Apply from saving. Previous settings remain active. If a saved shortcut is unavailable at startup, the app reports it and registers the other available shortcuts.

### File format

The default settings serialize as:

```json
{
  "Display": null,
  "Normal": {
    "Modifiers": 7,
    "Key": 38
  },
  "Left": {
    "Modifiers": 7,
    "Key": 37
  },
  "UpsideDown": {
    "Modifiers": 7,
    "Key": 40
  },
  "Right": {
    "Modifiers": 7,
    "Key": 39
  },
  "StartMinimized": false
}
```

| Field | Meaning |
| --- | --- |
| `Display` | `null` follows whichever display is currently primary. A Windows device name such as `\\\\.\\DISPLAY1` in JSON selects a specific display. If it is disconnected, rotation reports an error until you choose a connected display and Apply. |
| `Normal` | Shortcut for the display's natural/default orientation. |
| `Left` | Shortcut for 90° counterclockwise from the default orientation. |
| `UpsideDown` | Shortcut for 180° from the default orientation. |
| `Right` | Shortcut for 90° clockwise from the default orientation. |
| `StartMinimized` | `true` starts the app in the tray without opening Settings. Missing values default to `false`. |

Each shortcut has two numeric fields:

- `Modifiers` is the sum of the desired modifier flags: **Alt = 1**, **Ctrl = 2**, **Shift = 4**, **Win = 8**. For example, `7` means Ctrl + Alt + Shift, and `3` means Ctrl + Alt.
- `Key` is the numeric Windows Forms `Keys` value for the main key. The arrow values are **Left = 37**, **Up = 38**, **Right = 39**, **Down = 40**. Other supported values are Space (`32`), Page Up (`33`), Page Down (`34`), End (`35`), Home (`36`), Insert (`45`), Delete (`46`), 0–9 (`48`–`57`), A–Z (`65`–`90`), NumPad 0–9 (`96`–`105`), F1–F11 (`112`–`122`), and F13–F24 (`124`–`135`). F12 is reserved and is not supported.

An enabled shortcut requires at least one modifier. To disable an action, set both `Modifiers` and `Key` to `0`, or select **Disabled** in the GUI.

For manual edits, exit the app from the tray first, preserve the property names and numeric values, then restart it. Settings are read at startup; the app does not watch the file for changes. Editing through the GUI is the easiest way to choose valid combinations.

The JSON file does not control the screen's current orientation or whether the app starts with Windows. Windows manages the current orientation through the display driver, and the Startup folder shortcut controls launch at sign-in.

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

## Publishing a release

Create an annotated tag in `vMAJOR.MINOR.PATCH` format on the commit to release, with the release notes as its annotation. Push the tag to GitHub. The **Release** workflow builds the tagged source and publishes `ScreenRotate.exe` and `ScreenRotate.exe.sha256` using the tag annotation as the release notes.
