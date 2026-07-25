# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run

# Publish as single-file executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

There are no tests in this project.

## Architecture

A **screen crosshair overlay** for Windows — a system tray app that draws a customizable crosshair/aim-point on top of all other windows, primarily for gaming.

- **Framework**: .NET 8.0, Windows Forms (`UseWindowsForms=true`), High DPI `PerMonitorV2`
- **UI paradigm**: No main window. The app lives in the **system tray** (`NotifyIcon`) and runs via a custom `ApplicationContext`. The crosshair is a borderless, click-through **layered window**; settings open in a separate `SettingsForm` dialog.

### Key files

| File | Role |
|---|---|
| `Program.cs` | Entry point, single-instance mutex, `CrosshairApplicationContext`, low-level keyboard hook (`WH_KEYBOARD_LL`) |
| `CrosshairForm.cs` | The transparent overlay — draws via GDI+ to an offscreen bitmap, then calls Win32 `UpdateLayeredWindow` for per-pixel alpha |
| `SettingsForm.cs` | Dark-themed settings window with trackbars/textboxes for all parameters, grouped into style, outline/center-dot, and offset sections |
| `SettingsManager.cs` | Static class that loads/saves `CrosshairSettings` as JSON (`settings.json` in the exe directory) and manages auto-start via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| `resources/` | Icons in multiple sizes (`crosshair.ico`, plus PNGs at 16–256px); all copied to output directory on build |

### Rendering pipeline

`CrosshairForm` is a `Form` with `WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE`, making it an always-on-top, click-through overlay hidden from Alt+Tab. Each redraw:

1. Clears an offscreen `Bitmap` (32-bit ARGB) to transparent
2. Draws the selected style using GDI+ (`Graphics`), with optional outline and center dot, each with their own color/thickness/shape settings
3. Passes the bitmap to `UpdateLayeredWindow` via `BLENDFUNCTION` with `AC_SRC_ALPHA` for per-pixel alpha

**Smart anti-aliasing**: AA is only enabled when the style actually needs it — circles/dots always use it, crosses only when rotated (non-orthogonal lines), squares never. This avoids the performance cost of AA on pixel-aligned lines.

### Supported styles

- **Crosshair** — radial arms (configurable count 2–12), with inner gap, arm length, rotation angle, optional center dot
- **Dot** — filled ellipse (configurable size)
- **Circle** — outlined ellipse (configurable size, thickness)
- **Square** — outlined or filled rectangle (configurable width/height, fill toggle, optional center dot)

### Settings persistence

`SettingsManager.Save()` writes JSON and updates the Windows registry for auto-start. All properties on `CrosshairSettings` have sensible defaults. The settings form calls `Save()` on every change and on close; closing the settings window hides it rather than destroying it.

### Global hotkey

A low-level keyboard hook (`SetWindowsHookEx` with `WH_KEYBOARD_LL`) listens for the configured hotkey (default `Ctrl+Q`) to toggle crosshair visibility. The hook callback checks modifier key state via `GetAsyncKeyState` and compares against the parsed hotkey string.
