using System.Diagnostics;
using System.IO;

namespace Killer.Services;

public class KillOutcome
{
    public List<string> KilledProcessNames { get; } = new();

    public List<string> ElevatedProcessNames { get; } = new();

    public List<string> StillRunningNames { get; } = new();

    public bool ElevationWasNeeded => ElevatedProcessNames.Count > 0 || StillRunningNames.Count > 0;

    public int TotalKilled => KilledProcessNames.Count + ElevatedProcessNames.Count;
}

public class ProcessKillerService
{
    public KillOutcome KillAll(IEnumerable<string> processNames)
    {
        var outcome = new KillOutcome();
        var needsElevation = new List<string>();

        foreach (var rawName in processNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var baseName = StripExeExtension(rawName);
            var processes = Process.GetProcessesByName(baseName);
            if (processes.Length == 0)
            {
                continue;
            }

            var anyLeftRunning = false;
            foreach (var process in processes)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                    if (!process.HasExited)
                    {
                        anyLeftRunning = true;
                    }
                }
                catch
                {
                    // Access denied or the process protects itself: fall back to the elevated helper.
                    anyLeftRunning = true;
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (anyLeftRunning)
            {
                needsElevation.Add(rawName);
            }
            else
            {
                outcome.KilledProcessNames.Add(rawName);
            }
        }

        if (needsElevation.Count > 0)
        {
            RunElevatedHelper(needsElevation);

            foreach (var name in needsElevation)
            {
                var stillRunning = Process.GetProcessesByName(StripExeExtension(name)).Length > 0;
                (stillRunning ? outcome.StillRunningNames : outcome.ElevatedProcessNames).Add(name);
            }
        }

        return outcome;
    }

    private static void RunElevatedHelper(List<string> processNames)
    {
        var helperPath = Path.Combine(AppContext.BaseDirectory, "Killer.Helper.exe");
        if (!File.Exists(helperPath))
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = helperPath,
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        foreach (var name in processNames)
        {
            startInfo.ArgumentList.Add(name);
        }

        try
        {
            using var helperProcess = Process.Start(startInfo);
            helperProcess?.WaitForExit(15000);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The user declined the admin (UAC) prompt. Those processes simply stay running.
        }
    }

    private static string StripExeExtension(string name) =>
        name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
}
