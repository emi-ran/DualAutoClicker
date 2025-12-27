using System.Text.Json;
using DualAutoClicker.Models;

namespace DualAutoClicker.Services;

/// <summary>
/// Manages loading and saving settings to %LOCALAPPDATA%
/// </summary>
public class SettingsService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DualAutoClicker"
    );

    private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ClickerSettings _settings = new();

    public ClickerSettings Settings => _settings;

    /// <summary>
    /// Load settings from disk, or create default if not exists
    /// </summary>
    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                _settings = JsonSerializer.Deserialize<ClickerSettings>(json, JsonOptions) ?? new();
            }
        }
        catch
        {
            // If any error, use defaults
            _settings = new ClickerSettings();
        }
    }

    /// <summary>
    /// Save current settings to disk
    /// </summary>
    public void Save()
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(AppDataFolder);

            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Silently fail - settings are not critical
        }
    }

    /// <summary>
    /// Update settings and auto-save
    /// </summary>
    public void Update(Action<ClickerSettings> updateAction)
    {
        updateAction(_settings);
        Save();
    }
}
