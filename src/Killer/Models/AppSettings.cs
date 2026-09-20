using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Killer.Models;

public class AppSettings
{
    public ObservableCollection<ProcessTarget> Processes { get; set; } = new();

    public bool StartWithWindows { get; set; }

    public ModifierKeys HotkeyModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Shift;

    public Key HotkeyKey { get; set; } = Key.K;

    public bool HotkeyEnabled { get; set; } = true;

    public bool AutoKillEnabled { get; set; }

    public double AutoKillIntervalHours { get; set; } = 1;

    public bool ShowNotificationOnKill { get; set; } = true;

    public double? WindowWidth { get; set; }

    public double? WindowHeight { get; set; }
}
