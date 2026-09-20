# Killer

A simple, clean process-cleanup tool for Windows, built with WPF on .NET 8. One click (or one
hotkey) closes a list of apps you choose.

## Features

- A list of process names to kill, managed in Settings - type a name, or click "Browse..." to
  pick a program file and add it by its actual filename.
- Import / Export the process list as JSON, so it's easy to share or back up.
- "Kill Now" button, plus a global hotkey (default Ctrl+Shift+K) that works from anywhere, even
  with the window closed.
- Kills run without admin rights first. If a process refuses to close, Killer asks Windows for
  admin rights (one UAC prompt) and finishes the job through a small helper program,
  `Killer.Helper.exe` - the main app itself never needs to run as admin.
- Optional auto-kill on a timer (off by default; set the number of hours in Settings).
- Runs from the system tray. Left-click the tray icon to open the window. Start with Windows,
  close to tray - both in Settings.
- A short Windows notification after each kill, showing how many processes were closed.

## What this app does not do

- No network access of any kind. Nothing is sent anywhere, ever.
- No telemetry, analytics, or update checks.

## Data and privacy

All data stays on your machine, under your own user account:

- Settings: `%APPDATA%\Killer\settings.json`, plain JSON, human-readable.
- "Start with Windows" writes one value under
  `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`. No admin rights are used
  or required for this, and nothing is written to `HKEY_LOCAL_MACHINE`.
- Admin rights are only ever requested at the moment you press Kill, and only if a process
  couldn't be closed without them.

## Building

Requires the .NET 8 SDK (`dotnet --version` should report 8.x).

- `build.bat` (or `build.ps1`) - cleans, then publishes slim, framework-dependent, single-file
  production exes for both `Killer.exe` and `Killer.Helper.exe` into `publish\`.
  "Framework-dependent" means the target machine needs the .NET 8 Desktop Runtime already
  installed - that's what keeps the output small instead of bundling the whole runtime.
- `run.bat` (or `run.ps1`) - stops any already-running instance, cleans, builds both projects,
  and runs the app straight from source in dev mode (`dotnet run`, Debug config).

## Project layout

```
src/Killer/           the tray app (WPF, .NET 8)
  Models/               ProcessTarget, AppSettings - what's persisted
  Services/             kill logic, settings, auto-start, global hotkey, auto-kill timer
  MainWindow.xaml(.cs)   the window - Processes and Settings tabs
src/Killer.Helper/    tiny elevated console app, only launched when a kill needs admin rights
```
