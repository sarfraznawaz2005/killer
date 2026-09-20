namespace Killer.Services;

public class AutoKillTimerService : IDisposable
{
    private System.Timers.Timer? _timer;

    public event Action? Elapsed;

    public void Start(double intervalHours)
    {
        Stop();

        if (intervalHours <= 0)
        {
            return;
        }

        _timer = new System.Timers.Timer(TimeSpan.FromHours(intervalHours))
        {
            AutoReset = true,
        };
        _timer.Elapsed += (_, _) => Elapsed?.Invoke();
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose() => Stop();
}
