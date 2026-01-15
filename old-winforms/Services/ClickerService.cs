using DualAutoClicker.Models;
using DualAutoClicker.Native;

namespace DualAutoClicker.Services;

/// <summary>
/// Core clicker automation service managing both left and right click operations
/// </summary>
public class ClickerService : IDisposable
{
    private readonly MouseHook _mouseHook;
    private readonly KeyboardHook _keyboardHook;
    private readonly SettingsService _settingsService;

    private readonly PrecisionClicker _leftClicker;
    private readonly PrecisionClicker _rightClicker;

    private bool _leftClickActive;
    private bool _rightClickActive;
    private bool _leftToggleState;
    private bool _rightToggleState;

    // Master toggle state
    private bool _masterEnabled = true;

    private bool _disposed;

    /// <summary>
    /// Fired when clicking state changes (for tray icon updates)
    /// </summary>
    public event Action<bool>? ClickingStateChanged;

    /// <summary>
    /// Fired when left clicking state changes (for UI animations)
    /// </summary>
    public event Action<bool>? LeftClickingChanged;

    /// <summary>
    /// Fired when right clicking state changes (for UI animations)
    /// </summary>
    public event Action<bool>? RightClickingChanged;

    /// <summary>
    /// Fired when master toggle state changes
    /// </summary>
    public event Action<bool>? MasterStateChanged;

    public bool IsClicking => _leftClickActive || _rightClickActive;
    public bool MasterEnabled => _masterEnabled;

    public ClickerService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        _mouseHook = new MouseHook();
        _keyboardHook = new KeyboardHook();

        // Use precision clickers instead of timers
        _leftClicker = new PrecisionClicker(InputSimulator.LeftClick);
        _rightClicker = new PrecisionClicker(InputSimulator.RightClick);

        // Wire up mouse hook events
        _mouseHook.XButton1StateChanged += isDown => OnMouseButtonStateChanged(4, isDown);
        _mouseHook.XButton2StateChanged += isDown => OnMouseButtonStateChanged(5, isDown);
        _mouseHook.MiddleButtonStateChanged += isDown => OnMouseButtonStateChanged(3, isDown);

        // Wire up keyboard hook events
        _keyboardHook.KeyStateChanged += OnKeyboardStateChanged;

        // Apply window targeting settings
        UpdateWindowTargeting();
    }

    public void UpdateWindowTargeting()
    {
        var wt = _settingsService.Settings.WindowTarget;
        InputSimulator.WindowTargetEnabled = wt.Enabled;
        InputSimulator.TargetProcessName = wt.ProcessName;
        InputSimulator.TargetWindowTitle = wt.WindowTitle;
    }

    /// <summary>
    /// Start listening for activation keys
    /// </summary>
    public void Start()
    {
        PrecisionClicker.SetHighResolution();
        _mouseHook.Install();
        _keyboardHook.Install();
    }

    /// <summary>
    /// Stop all clicking and unhook
    /// </summary>
    public void Stop()
    {
        StopLeftClicking();
        StopRightClicking();
        _mouseHook.Uninstall();
        _keyboardHook.Uninstall();
    }

    private void OnMouseButtonStateChanged(int buttonCode, bool isDown)
    {
        // Check master toggle
        var masterSettings = _settingsService.Settings.MasterToggle;
        if (masterSettings.Enabled && masterSettings.KeyType == "mouse" && masterSettings.KeyCode == buttonCode)
        {
            if (isDown) ToggleMaster();
            return;
        }

        if (!_masterEnabled) return;

        var leftSettings = _settingsService.Settings.LeftClick;
        var rightSettings = _settingsService.Settings.RightClick;

        // Check left click activation
        if (leftSettings.Enabled && leftSettings.KeyType == "mouse" && leftSettings.KeyCode == buttonCode)
        {
            HandleActivation(isDown, leftSettings.Mode, ref _leftToggleState,
                () => StartLeftClicking(leftSettings.Cps, leftSettings.RandomPercent), StopLeftClicking);
        }

        // Check right click activation
        if (rightSettings.Enabled && rightSettings.KeyType == "mouse" && rightSettings.KeyCode == buttonCode)
        {
            HandleActivation(isDown, rightSettings.Mode, ref _rightToggleState,
                () => StartRightClicking(rightSettings.Cps, rightSettings.RandomPercent), StopRightClicking);
        }
    }

    private void OnKeyboardStateChanged(int vkCode, bool isDown)
    {
        // Check master toggle
        var masterSettings = _settingsService.Settings.MasterToggle;
        if (masterSettings.Enabled && masterSettings.KeyType == "keyboard" && masterSettings.KeyCode == vkCode)
        {
            if (isDown) ToggleMaster();
            return;
        }

        if (!_masterEnabled) return;

        var leftSettings = _settingsService.Settings.LeftClick;
        var rightSettings = _settingsService.Settings.RightClick;

        // Check left click activation
        if (leftSettings.Enabled && leftSettings.KeyType == "keyboard" && leftSettings.KeyCode == vkCode)
        {
            HandleActivation(isDown, leftSettings.Mode, ref _leftToggleState,
                () => StartLeftClicking(leftSettings.Cps, leftSettings.RandomPercent), StopLeftClicking);
        }

        // Check right click activation
        if (rightSettings.Enabled && rightSettings.KeyType == "keyboard" && rightSettings.KeyCode == vkCode)
        {
            HandleActivation(isDown, rightSettings.Mode, ref _rightToggleState,
                () => StartRightClicking(rightSettings.Cps, rightSettings.RandomPercent), StopRightClicking);
        }
    }

    private void ToggleMaster()
    {
        _masterEnabled = !_masterEnabled;

        // Stop all clicking when master is disabled
        if (!_masterEnabled)
        {
            StopLeftClicking();
            StopRightClicking();
            _leftToggleState = false;
            _rightToggleState = false;
        }

        MasterStateChanged?.Invoke(_masterEnabled);
    }

    public void SetMasterEnabled(bool enabled)
    {
        if (_masterEnabled == enabled) return;
        _masterEnabled = enabled;

        if (!_masterEnabled)
        {
            StopLeftClicking();
            StopRightClicking();
            _leftToggleState = false;
            _rightToggleState = false;
        }

        MasterStateChanged?.Invoke(_masterEnabled);
    }

    private void HandleActivation(bool isDown, ActivationMode mode, ref bool toggleState,
        Action startAction, Action stopAction)
    {
        if (mode == ActivationMode.Hold)
        {
            if (isDown) startAction();
            else stopAction();
        }
        else // Toggle mode
        {
            if (isDown)
            {
                toggleState = !toggleState;
                if (toggleState) startAction();
                else stopAction();
            }
        }
    }

    private void StartLeftClicking(int cps, int randomPercent)
    {
        if (_leftClickActive) return;
        _leftClickActive = true;
        _leftClicker.Start(cps, randomPercent);
        ClickingStateChanged?.Invoke(IsClicking);
        LeftClickingChanged?.Invoke(true);
    }

    private void StopLeftClicking()
    {
        if (!_leftClickActive) return;
        _leftClickActive = false;
        _leftClicker.Stop();
        ClickingStateChanged?.Invoke(IsClicking);
        LeftClickingChanged?.Invoke(false);
    }

    private void StartRightClicking(int cps, int randomPercent)
    {
        if (_rightClickActive) return;
        _rightClickActive = true;
        _rightClicker.Start(cps, randomPercent);
        ClickingStateChanged?.Invoke(IsClicking);
        RightClickingChanged?.Invoke(true);
    }

    private void StopRightClicking()
    {
        if (!_rightClickActive) return;
        _rightClickActive = false;
        _rightClicker.Stop();
        ClickingStateChanged?.Invoke(IsClicking);
        RightClickingChanged?.Invoke(false);
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        PrecisionClicker.ResetResolution();

        _leftClicker.Dispose();
        _rightClicker.Dispose();
        _mouseHook.Dispose();
        _keyboardHook.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
