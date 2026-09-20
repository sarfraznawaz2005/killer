using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Killer.Models;

namespace Killer.Services;

public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Killer");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // Corrupt or unreadable settings file: fall back to defaults instead of crashing.
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    public void ExportProcesses(IEnumerable<ProcessTarget> processes, string filePath)
    {
        var json = JsonSerializer.Serialize(processes.ToList(), JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public List<ProcessTarget> ImportProcesses(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<ProcessTarget>>(json, JsonOptions) ?? new List<ProcessTarget>();
    }
}
