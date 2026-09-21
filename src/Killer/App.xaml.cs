using System.Linq;
using System.Windows;
using Killer.Models;
using Killer.Services;

namespace Killer;

public partial class App : Application
{
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private GlobalHotkeyService? _hotkeyService;
    private AutoKillTimerService? _autoKillTimer;
    private Mutex? _singleInstanceMutex;
    private MainWindow? _mainWindow;
    private bool _isExiting;

    public SettingsService SettingsService { get; } = new();

    public AppSettings Settings { get; private set; } = new();

    public ProcessKillerService KillerService { get; } = new();

    public AutoStartService AutoStartService { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, "Killer.SingleInstance.90b3f1b2", out var isNew);
        if (!isNew)
        {
            MessageBox.Show("Killer is already running. Look for its icon in the system tray.",
                "Killer", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Settings = SettingsService.Load();

        _mainWindow = new MainWindow();
        _mainWindow.Initialize(this);
        _mainWindow.Closing += (_, args) =>
        {
            if (_isExiting)
            {
                return;
            }

            args.Cancel = true;
            HideMainWindow();
        };

        SetupTrayIcon();
        SetupHotkey();
        SetupAutoKillTimer();

        // Killer always starts minimized to the tray - opening the window is an explicit
        // action (tray icon click, tray menu, or the global hotkey doesn't open it either).
    }

    public void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.CenterOnScreen();
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void HideMainWindow() => _mainWindow?.Hide();

    private void SetupTrayIcon()
    {
        var icon = Environment.ProcessPath is { } path
            ? System.Drawing.Icon.ExtractAssociatedIcon(path)
            : null;

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = icon ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Killer",
        };
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ShowMainWindow();
            }
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Kill Now", null, (_, _) => PerformKill());
        menu.Items.Add("Open Killer", null, (_, _) => ShowMainWindow());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _notifyIcon.ContextMenuStrip = menu;
    }

    private void SetupHotkey()
    {
        _hotkeyService = new GlobalHotkeyService();
        _hotkeyService.HotkeyPressed += () => Dispatcher.Invoke(PerformKill);
        ApplyHotkeySetting();
    }

    public void ApplyHotkeySetting()
    {
        if (_hotkeyService is null || _mainWindow is null)
        {
            return;
        }

        _hotkeyService.Unregister();
        if (Settings.HotkeyEnabled)
        {
            _hotkeyService.Register(_mainWindow, Settings.HotkeyModifiers, Settings.HotkeyKey);
        }
    }

    private void SetupAutoKillTimer()
    {
        _autoKillTimer = new AutoKillTimerService();
        _autoKillTimer.Elapsed += () => Dispatcher.Invoke(PerformKill);
        ApplyAutoKillSetting();
    }

    public void ApplyAutoKillSetting()
    {
        if (_autoKillTimer is null)
        {
            return;
        }

        if (Settings.AutoKillEnabled)
        {
            _autoKillTimer.Start(Settings.AutoKillIntervalHours);
        }
        else
        {
            _autoKillTimer.Stop();
        }
    }

    public void PerformKill()
    {
        var names = Settings.Processes.Where(p => p.IsEnabled).Select(p => p.Name).ToList();
        if (names.Count == 0)
        {
            ShowBalloon("Killer", "No processes are set up yet. Add some in Settings.");
            return;
        }

        var outcome = KillerService.KillAll(names);

        if (Settings.ShowNotificationOnKill)
        {
            string message;
            if (outcome.TotalKilled == 0)
            {
                message = "Nothing on your list was running.";
            }
            else
            {
                var killedNames = outcome.KilledProcessNames.Concat(outcome.ElevatedProcessNames);
                message = $"Killed {outcome.TotalKilled} process(es):\n" +
                    string.Join("\n", killedNames.Select(name => $"- {name}"));
            }

            if (outcome.StillRunningNames.Count > 0)
            {
                message += $"\n{outcome.StillRunningNames.Count} could not be killed:\n" +
                    string.Join("\n", outcome.StillRunningNames.Select(name => $"- {name}"));
            }

            ShowBalloon("Killer", message);
        }

        _mainWindow?.OnKillPerformed(outcome);
    }

    public void ShowBalloon(string title, string message) =>
        _notifyIcon?.ShowBalloonTip(4000, title, message, System.Windows.Forms.ToolTipIcon.Info);

    public void SaveSettings() => SettingsService.Save(Settings);

    public void ExitApplication()
    {
        _isExiting = true;
        SettingsService.Save(Settings);

        _hotkeyService?.Dispose();
        _autoKillTimer?.Dispose();

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        _singleInstanceMutex?.ReleaseMutex();
        Shutdown();
    }
}
