using System.Diagnostics;

namespace Killer.Helper;

// This program only does one job: kill the process names it is given, then exit.
// It always runs elevated (see app.manifest), so it is used only when a normal,
// non-admin kill attempt fails.
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return 0;
        }

        var failures = 0;

        foreach (var processName in args)
        {
            var name = processName.Trim();
            if (name.Length == 0)
            {
                continue;
            }

            var nameWithoutExtension = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? name[..^4]
                : name;

            foreach (var process in Process.GetProcessesByName(nameWithoutExtension))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
                catch
                {
                    failures++;
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        return failures == 0 ? 0 : 1;
    }
}
