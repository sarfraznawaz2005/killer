using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Killer.Models;

public class ProcessTarget : INotifyPropertyChanged
{
    private bool _isRunning;

    public string Name { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    [JsonIgnore]
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning == value) return;
            _isRunning = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
