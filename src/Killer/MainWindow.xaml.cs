using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Killer.Models;
using Killer.Services;
using Microsoft.Win32;

namespace Killer;

public partial class MainWindow : Window
{
    private App _app = null!;
    private ModifierKeys _pendingHotkeyModifiers;
    private Key _pendingHotkeyKey;

    public MainWindow()
    {
        InitializeComponent();
        WindowStyleHelper.DisableMaximize(this);
    }

    public void Initialize(App app)
    {
        _app = app;

        if (_app.Settings.WindowWidth is { } width && width >= MinWidth)
        {
            Width = width;
        }

        if (_app.Settings.WindowHeight is { } height && height >= MinHeight)
        {
            Height = height;
        }

        SizeChanged += (_, _) =>
        {
            if (WindowState == WindowState.Normal)
            {
                _app.Settings.WindowWidth = Width;
                _app.Settings.WindowHeight = Height;
            }
        };

        ProcessListView.ItemsSource = _app.Settings.Processes;

        StartWithWindowsCheck.IsChecked = _app.Settings.StartWithWindows;
        HotkeyEnabledCheck.IsChecked = _app.Settings.HotkeyEnabled;
        ShowNotificationCheck.IsChecked = _app.Settings.ShowNotificationOnKill;
        AutoKillEnabledCheck.IsChecked = _app.Settings.AutoKillEnabled;
        AutoKillHoursBox.Text = _app.Settings.AutoKillIntervalHours.ToString();

        _pendingHotkeyModifiers = _app.Settings.HotkeyModifiers;
        _pendingHotkeyKey = _app.Settings.HotkeyKey;
        var hotkeyText = FormatHotkey(_pendingHotkeyModifiers, _pendingHotkeyKey);
        HotkeyBox.Text = hotkeyText;
        HotkeyHintText.Text = $"Hotkey: {hotkeyText}";
    }

    public void CenterOnScreen()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;
    }

    private void KillNowButton_Click(object sender, RoutedEventArgs e) => _app.PerformKill();

    public void OnKillPerformed(KillOutcome outcome)
    {
        StatusText.Text = outcome.TotalKilled == 0
            ? "Ready. Nothing on your list was running."
            : outcome.StillRunningNames.Count > 0
                ? $"Killed {outcome.TotalKilled}. {outcome.StillRunningNames.Count} would not close."
                : $"Killed {outcome.TotalKilled} process(es).";
    }

    private void NewProcessNameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddProcessFromTextBox();
        }
    }

    private void AddProcessButton_Click(object sender, RoutedEventArgs e) => AddProcessFromTextBox();

    private void AddProcessFromTextBox()
    {
        var name = NewProcessNameBox.Text.Trim();
        if (name.Length == 0)
        {
            return;
        }

        AddProcessName(name);
        NewProcessNameBox.Clear();
    }

    private void AddProcessName(string name)
    {
        if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            name += ".exe";
        }

        var alreadyExists = _app.Settings.Processes
            .Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            StatusText.Text = $"\"{name}\" is already on the list.";
            return;
        }

        _app.Settings.Processes.Add(new ProcessTarget { Name = name, IsEnabled = true });
    }

    private void BrowseProcessButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a program to add",
            Filter = "Programs (*.exe)|*.exe|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            AddProcessName(Path.GetFileName(dialog.FileName));
        }
    }

    private void RemoveProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProcessTarget target })
        {
            _app.Settings.Processes.Remove(target);
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import process list",
            Filter = "Killer process list (*.json)|*.json|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var imported = _app.SettingsService.ImportProcesses(dialog.FileName);
            var addedCount = 0;

            foreach (var target in imported)
            {
                if (string.IsNullOrWhiteSpace(target.Name))
                {
                    continue;
                }

                var alreadyExists = _app.Settings.Processes
                    .Any(p => string.Equals(p.Name, target.Name, StringComparison.OrdinalIgnoreCase));

                if (!alreadyExists)
                {
                    _app.Settings.Processes.Add(target);
                    addedCount++;
                }
            }

            StatusText.Text = $"Imported {addedCount} new process(es).";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not read that file.\n\n{ex.Message}", "Import failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export process list",
            Filter = "Killer process list (*.json)|*.json|All files (*.*)|*.*",
            FileName = "killer-processes.json",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _app.SettingsService.ExportProcesses(_app.Settings.Processes, dialog.FileName);
            StatusText.Text = "Process list exported.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save that file.\n\n{ex.Message}", "Export failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        var isModifierOnly = key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System;

        if (isModifierOnly)
        {
            e.Handled = true;
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None)
        {
            // Require at least one modifier so the hotkey doesn't collide with normal typing elsewhere.
            e.Handled = true;
            return;
        }

        _pendingHotkeyModifiers = modifiers;
        _pendingHotkeyKey = key;
        HotkeyBox.Text = FormatHotkey(modifiers, key);
        e.Handled = true;
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _app.Settings.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        _app.Settings.HotkeyEnabled = HotkeyEnabledCheck.IsChecked == true;
        _app.Settings.HotkeyModifiers = _pendingHotkeyModifiers;
        _app.Settings.HotkeyKey = _pendingHotkeyKey;
        _app.Settings.ShowNotificationOnKill = ShowNotificationCheck.IsChecked == true;
        _app.Settings.AutoKillEnabled = AutoKillEnabledCheck.IsChecked == true;

        if (double.TryParse(AutoKillHoursBox.Text, out var hours) && hours > 0)
        {
            _app.Settings.AutoKillIntervalHours = hours;
        }

        _app.SaveSettings();
        _app.AutoStartService.SetEnabled(_app.Settings.StartWithWindows);
        _app.ApplyHotkeySetting();
        _app.ApplyAutoKillSetting();

        HotkeyHintText.Text = $"Hotkey: {FormatHotkey(_pendingHotkeyModifiers, _pendingHotkeyKey)}";
        SaveHintText.Text = "Saved.";
    }

    private static string FormatHotkey(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}
