namespace DualAutoClicker.Models;

/// <summary>
/// Represents settings for a single clicker (left or right)
/// </summary>
public class SingleClickerSettings
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The activation key type: "mouse" or "keyboard"
    /// </summary>
    public string KeyType { get; set; } = "mouse";

    /// <summary>
    /// The key code (e.g., 4 for XButton1, 5 for XButton2, or VK codes for keyboard)
    /// </summary>
    public int KeyCode { get; set; } = 4; // XButton1 (MB4) by default

    /// <summary>
    /// Display name for the key
    /// </summary>
    public string KeyName { get; set; } = "MB4";

    public ActivationMode Mode { get; set; } = ActivationMode.Hold;
    public int Cps { get; set; } = 20;

    /// <summary>
    /// Randomization percentage (0-30)
    /// </summary>
    public int RandomPercent { get; set; } = 0;
}

/// <summary>
/// A profile containing clicker settings
/// </summary>
public class ProfileSettings
{
    public string Name { get; set; } = "Profil";

    public SingleClickerSettings LeftClick { get; set; } = new()
    {
        Enabled = true,
        KeyType = "mouse",
        KeyCode = 5, // XButton2 (MB5)
        KeyName = "MB5",
        Mode = ActivationMode.Hold,
        Cps = 20,
        RandomPercent = 0
    };

    public SingleClickerSettings RightClick { get; set; } = new()
    {
        Enabled = true,
        KeyType = "mouse",
        KeyCode = 4, // XButton1 (MB4)
        KeyName = "MB4",
        Mode = ActivationMode.Hold,
        Cps = 20,
        RandomPercent = 0
    };

    /// <summary>
    /// Master toggle key settings
    /// </summary>
    public MasterToggleSettings MasterToggle { get; set; } = new();

    /// <summary>
    /// Target window settings
    /// </summary>
    public WindowTargetSettings WindowTarget { get; set; } = new();

    /// <summary>
    /// Keyboard macro settings
    /// </summary>
    public KeyboardMacroSettings KeyboardMacro { get; set; } = new();

    /// <summary>
    /// Creates a default profile with specified index
    /// </summary>
    public static ProfileSettings CreateDefault(int index)
    {
        return new ProfileSettings
        {
            Name = $"Profil {index + 1}",
            LeftClick = new SingleClickerSettings
            {
                Enabled = true,
                KeyType = "mouse",
                KeyCode = 5, // XButton2 (MB5)
                KeyName = "MB5",
                Mode = ActivationMode.Hold,
                Cps = 20,
                RandomPercent = 0
            },
            RightClick = new SingleClickerSettings
            {
                Enabled = true,
                KeyType = "mouse",
                KeyCode = 4, // XButton1 (MB4)
                KeyName = "MB4",
                Mode = ActivationMode.Hold,
                Cps = 20,
                RandomPercent = 0
            },
            MasterToggle = new MasterToggleSettings
            {
                Enabled = false,
                KeyType = "keyboard",
                KeyCode = 0x77, // F8
                KeyName = "F8"
            },
            WindowTarget = new WindowTargetSettings
            {
                Enabled = false,
                ProcessName = ""
            },
            KeyboardMacro = new KeyboardMacroSettings
            {
                Enabled = false,
                KeyType = "keyboard",
                KeyCode = 0,
                KeyName = "Yok",
                BaseText = "",
                MinRandomChars = 3,
                MaxRandomChars = 10,
                JunkCharacters = ":;*!'.,:\"~"
            }
        };
    }
}

/// <summary>
/// Root settings containing profiles and global settings
/// </summary>
public class ClickerSettings
{
    /// <summary>
    /// All 6 profiles
    /// </summary>
    public List<ProfileSettings> Profiles { get; set; } = new()
    {
        ProfileSettings.CreateDefault(0),
        ProfileSettings.CreateDefault(1),
        ProfileSettings.CreateDefault(2),
        ProfileSettings.CreateDefault(3),
        ProfileSettings.CreateDefault(4),
        ProfileSettings.CreateDefault(5)
    };

    /// <summary>
    /// Currently active profile index (0-5)
    /// </summary>
    public int ActiveProfileIndex { get; set; } = 0;

    /// <summary>
    /// Start with Windows (global, not per-profile)
    /// </summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// Gets the currently active profile
    /// </summary>
    public ProfileSettings ActiveProfile => Profiles[ActiveProfileIndex];

    // Legacy compatibility properties - redirect to active profile
    public SingleClickerSettings LeftClick => ActiveProfile.LeftClick;
    public SingleClickerSettings RightClick => ActiveProfile.RightClick;
    public MasterToggleSettings MasterToggle => ActiveProfile.MasterToggle;
    public WindowTargetSettings WindowTarget => ActiveProfile.WindowTarget;
    public KeyboardMacroSettings KeyboardMacro => ActiveProfile.KeyboardMacro;
}

/// <summary>
/// Master toggle key settings
/// </summary>
public class MasterToggleSettings
{
    public bool Enabled { get; set; } = false;
    public string KeyType { get; set; } = "keyboard";
    public int KeyCode { get; set; } = 0x77; // F8
    public string KeyName { get; set; } = "F8";
}

/// <summary>
/// Window targeting settings
/// </summary>
public class WindowTargetSettings
{
    public bool Enabled { get; set; } = false;
    public string ProcessName { get; set; } = "";
}

/// <summary>
/// Keyboard macro settings for anti-detection text output
/// </summary>
public class KeyboardMacroSettings
{
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The activation key type: "mouse" or "keyboard"
    /// </summary>
    public string KeyType { get; set; } = "keyboard";

    /// <summary>
    /// The key code (VK code for keyboard, button code for mouse)
    /// </summary>
    public int KeyCode { get; set; } = 0; // None by default

    /// <summary>
    /// Display name for the key
    /// </summary>
    public string KeyName { get; set; } = "Yok";

    /// <summary>
    /// Requires Alt modifier to be held
    /// </summary>
    public bool RequireAlt { get; set; } = false;

    /// <summary>
    /// Requires Shift modifier to be held
    /// </summary>
    public bool RequireShift { get; set; } = false;

    /// <summary>
    /// Requires Ctrl modifier to be held
    /// </summary>
    public bool RequireCtrl { get; set; } = false;

    /// <summary>
    /// The base text to send (user-defined)
    /// </summary>
    public string BaseText { get; set; } = "";

    /// <summary>
    /// Minimum number of random characters to append
    /// </summary>
    public int MinRandomChars { get; set; } = 3;

    /// <summary>
    /// Maximum number of random characters to append
    /// </summary>
    public int MaxRandomChars { get; set; } = 10;

    /// <summary>
    /// Available junk characters for randomization
    /// </summary>
    public string JunkCharacters { get; set; } = ":;*!'.,:\"~";

    /// <summary>
    /// Gets the full display name including modifiers
    /// </summary>
    public string FullKeyName
    {
        get
        {
            if (KeyCode == 0) return "Yok";

            var parts = new List<string>();
            if (RequireCtrl) parts.Add("Ctrl");
            if (RequireAlt) parts.Add("Alt");
            if (RequireShift) parts.Add("Shift");
            parts.Add(KeyName);
            return string.Join("+", parts);
        }
    }
}

/// <summary>
/// Activation modes
/// </summary>
public enum ActivationMode
{
    Hold,   // Click while key is held
    Toggle  // Toggle on/off with key press
}
