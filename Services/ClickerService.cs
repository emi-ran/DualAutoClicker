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
    private readonly KeyboardMacroService _macroService;

    private readonly PrecisionClicker _leftClicker;
    private readonly PrecisionClicker _rightClicker;

    private bool _leftClickActive;
    private bool _rightClickActive;
    private bool _leftToggleState;
    private bool _rightToggleState;

    // Master toggle state
    private bool _masterEnabled = true;

    // Macro state tracking (prevents repeated execution on key hold)
    private bool _macroKeyDown;
    private bool _macroMouseDown;

    // Keys whose key down we swallowed for the active macro combo; their key up must be
    // swallowed as well, otherwise applications are left with a stuck modifier.
    private readonly HashSet<int> _suppressedKeys = new();

    private bool _disposed;

    /// <summary>
    /// Fired when clicking state changes (for tray icon updates)
    /// </summary>
    public event Action<bool>? ClickingStateChanged;

    /// <summary>
    /// Fired when master toggle state changes
    /// </summary>
    public event Action<bool>? MasterStateChanged;

    public bool IsClicking => _leftClickActive || _rightClickActive;
    public bool IsLeftClicking => _leftClickActive;
    public bool IsRightClicking => _rightClickActive;
    public bool MasterEnabled => _masterEnabled;

    public ClickerService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _macroService = new KeyboardMacroService();

        _mouseHook = new MouseHook();
        _keyboardHook = new KeyboardHook();

        // Use precision clickers instead of timers
        _leftClicker = new PrecisionClicker(InputSimulator.LeftClick);
        _rightClicker = new PrecisionClicker(InputSimulator.RightClick);

        // Wire up mouse hook events
        _mouseHook.XButton1StateChanged += isDown => OnMouseButtonStateChanged(4, isDown);
        _mouseHook.XButton2StateChanged += isDown => OnMouseButtonStateChanged(5, isDown);
        _mouseHook.MiddleButtonStateChanged += isDown => OnMouseButtonStateChanged(3, isDown);

        // Wire up keyboard hook events with suppression support
        _keyboardHook.KeyStateChangedWithSuppress += OnKeyboardStateChangedWithSuppress;

        // Apply window targeting settings
        UpdateWindowTargeting();
    }

    public void UpdateWindowTargeting()
    {
        var wt = _settingsService.Settings.WindowTarget;
        InputSimulator.WindowTargetEnabled = wt.Enabled;
        InputSimulator.TargetProcessName = wt.ProcessName;
    }

    /// <summary>
    /// Start listening for activation keys
    /// </summary>
    public void Start()
    {
        ResetMacroState(resetModifierTracking: true);
        _mouseHook.Install();
        _keyboardHook.Install();
    }

    /// <summary>
    /// Stop all clicking and unhook
    /// </summary>
    public void Stop()
    {
        // Unhook first: while a low-level hook is installed, every system input event
        // is routed through this thread. Leaving it installed during shutdown makes
        // the mouse/keyboard feel frozen until the process is gone.
        _mouseHook.Uninstall();
        _keyboardHook.Uninstall();

        StopLeftClicking();
        StopRightClicking();
        ResetMacroState(resetModifierTracking: true);
    }

    /// <summary>
    /// Clears macro debounce/suppression state so no key stays logically held down.
    /// </summary>
    private void ResetMacroState(bool resetModifierTracking)
    {
        _suppressedKeys.Clear();
        _macroKeyDown = false;
        _macroMouseDown = false;

        if (resetModifierTracking)
        {
            _keyboardHook.ResetModifierState();
        }
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
        var macroSettings = _settingsService.Settings.KeyboardMacro;

        // Check keyboard macro activation (with modifier support for mouse too)
        if (macroSettings.Enabled && macroSettings.KeyType == "mouse" && macroSettings.KeyCode == buttonCode)
        {
            // Check if required modifiers are pressed
            bool modifiersMatch = CheckMacroModifiers(macroSettings);
            
            if (modifiersMatch)
            {
                // Only execute on initial button press, not on repeat
                if (isDown && !_macroMouseDown)
                {
                    _macroMouseDown = true;
                    _macroService.EnqueueMacro(macroSettings);
                }
                else if (!isDown)
                {
                    _macroMouseDown = false;
                }
            }
            return;
        }

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

    /// <summary>
    /// Handles keyboard state changes with support for key suppression
    /// Returns true if the key should be suppressed (blocked from other apps)
    /// </summary>
    private bool OnKeyboardStateChangedWithSuppress(int vkCode, bool isDown)
    {
        // Check master toggle
        var masterSettings = _settingsService.Settings.MasterToggle;
        if (masterSettings.Enabled && masterSettings.KeyType == "keyboard" && masterSettings.KeyCode == vkCode)
        {
            if (isDown) ToggleMaster();
            return false; // Don't suppress master toggle key
        }

        if (!_masterEnabled)
        {
            // Deliver the key up of anything we swallowed earlier
            if (!isDown && _suppressedKeys.Remove(vkCode)) return true;
            return false;
        }

        var leftSettings = _settingsService.Settings.LeftClick;
        var rightSettings = _settingsService.Settings.RightClick;
        var macroSettings = _settingsService.Settings.KeyboardMacro;
        bool macroActive = macroSettings.Enabled && macroSettings.KeyCode != 0;

        // Check if this is a modifier key that should be suppressed (part of active macro)
        if (KeyboardHook.IsModifierKey(vkCode))
        {
            if (!isDown)
            {
                // Always swallow the key up for a key whose key down we swallowed,
                // even if the macro settings changed in the meantime.
                return _suppressedKeys.Remove(vkCode);
            }

            // Suppress the modifier key down only when the macro actually requires it.
            if (macroActive && IsRequiredMacroModifier(vkCode, macroSettings))
            {
                _suppressedKeys.Add(vkCode);
                return true;
            }

            return false;
        }

        // Check keyboard macro activation (with modifier support)
        if (macroSettings.Enabled && macroSettings.KeyType == "keyboard" && macroSettings.KeyCode == vkCode)
        {
            // Check if required modifiers are pressed
            bool modifiersMatch = CheckMacroModifiers(macroSettings);
            
            if (modifiersMatch)
            {
                // Only execute on initial keydown, not on repeat
                if (isDown && !_macroKeyDown)
                {
                    _macroKeyDown = true;
                    // Queue the injection off the hook thread; the callback must return fast.
                    _macroService.EnqueueMacro(macroSettings);
                }
                else if (!isDown)
                {
                    _macroKeyDown = false;
                }
                // Suppress this key so it doesn't type in the application
                return true;
            }
        }

        // Reset macro key state if modifiers no longer match
        if (!isDown && _macroKeyDown && macroSettings.KeyCode == vkCode)
        {
            _macroKeyDown = false;
        }

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

        return false; // Don't suppress other keys
    }

    /// <summary>
    /// Returns true when the given modifier key is required by the macro combo
    /// </summary>
    private static bool IsRequiredMacroModifier(int vkCode, KeyboardMacroSettings settings)
    {
        return vkCode switch
        {
            0x12 or 0xA4 or 0xA5 => settings.RequireAlt,   // Alt
            0x10 or 0xA0 or 0xA1 => settings.RequireShift, // Shift
            0x11 or 0xA2 or 0xA3 => settings.RequireCtrl,  // Ctrl
            _ => false
        };
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
            ResetMacroState(resetModifierTracking: false);
        }

        MasterStateChanged?.Invoke(_masterEnabled);
    }

    /// <summary>
    /// Checks whether the currently held modifiers match the macro combo exactly.
    /// Extra modifiers must not trigger the macro, otherwise Ctrl/Alt combinations
    /// leak into the target application and can produce spurious shortcut/beep behavior.
    /// </summary>
    private bool CheckMacroModifiers(KeyboardMacroSettings macroSettings)
    {
        return _keyboardHook.IsAltDown == macroSettings.RequireAlt
            && _keyboardHook.IsShiftDown == macroSettings.RequireShift
            && _keyboardHook.IsCtrlDown == macroSettings.RequireCtrl;
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
            ResetMacroState(resetModifierTracking: false);
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

        // The 1 ms system timer is only needed while clicks are injected. Keeping it
        // active for the whole session raises timer/interrupt load for no benefit.
        PrecisionClicker.SetHighResolution();
        _leftClicker.Start(cps, randomPercent);
        ClickingStateChanged?.Invoke(IsClicking);
    }

    private void StopLeftClicking()
    {
        if (!_leftClickActive) return;
        _leftClickActive = false;
        _leftClicker.Stop();
        if (!IsClicking)
        {
            PrecisionClicker.ResetResolution();
        }
        ClickingStateChanged?.Invoke(IsClicking);
    }

    private void StartRightClicking(int cps, int randomPercent)
    {
        if (_rightClickActive) return;
        _rightClickActive = true;
        PrecisionClicker.SetHighResolution();
        _rightClicker.Start(cps, randomPercent);
        ClickingStateChanged?.Invoke(IsClicking);
    }

    private void StopRightClicking()
    {
        if (!_rightClickActive) return;
        _rightClickActive = false;
        _rightClicker.Stop();
        if (!IsClicking)
        {
            PrecisionClicker.ResetResolution();
        }
        ClickingStateChanged?.Invoke(IsClicking);
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        _leftClicker.Dispose();
        _rightClicker.Dispose();
        _macroService.Dispose();
        PrecisionClicker.ResetResolution();
        _mouseHook.Dispose();
        _keyboardHook.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
