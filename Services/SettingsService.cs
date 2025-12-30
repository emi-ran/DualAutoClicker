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

    public event Action<int>? ProfileChanged;

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

                // Ensure we have 6 profiles
                while (_settings.Profiles.Count < 6)
                {
                    _settings.Profiles.Add(ProfileSettings.CreateDefault(_settings.Profiles.Count));
                }

                // Validate active profile index
                if (_settings.ActiveProfileIndex < 0 || _settings.ActiveProfileIndex >= 6)
                {
                    _settings.ActiveProfileIndex = 0;
                }
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

    /// <summary>
    /// Switch to a different profile
    /// </summary>
    public void SwitchProfile(int profileIndex)
    {
        if (profileIndex < 0 || profileIndex >= 6) return;

        _settings.ActiveProfileIndex = profileIndex;
        Save();
        ProfileChanged?.Invoke(profileIndex);
    }

    /// <summary>
    /// Get the currently active profile
    /// </summary>
    public ProfileSettings GetActiveProfile()
    {
        return _settings.ActiveProfile;
    }

    /// <summary>
    /// Update the name of a profile
    /// </summary>
    public void UpdateProfileName(int profileIndex, string name)
    {
        if (profileIndex < 0 || profileIndex >= 6) return;

        _settings.Profiles[profileIndex].Name = name;
        Save();
    }
}

