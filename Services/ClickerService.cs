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

    private bool _disposed;

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
        var leftSettings = _settingsService.Settings.LeftClick;
        var rightSettings = _settingsService.Settings.RightClick;

        // Check left click activation
        if (leftSettings.Enabled && leftSettings.KeyType == "mouse" && leftSettings.KeyCode == buttonCode)
        {
            HandleActivation(isDown, leftSettings.Mode, ref _leftToggleState,
                () => StartLeftClicking(leftSettings.Cps), StopLeftClicking);
        }

        // Check right click activation
        if (rightSettings.Enabled && rightSettings.KeyType == "mouse" && rightSettings.KeyCode == buttonCode)
        {
            HandleActivation(isDown, rightSettings.Mode, ref _rightToggleState,
                () => StartRightClicking(rightSettings.Cps), StopRightClicking);
        }
    }

    private void OnKeyboardStateChanged(int vkCode, bool isDown)
    {
        var leftSettings = _settingsService.Settings.LeftClick;
        var rightSettings = _settingsService.Settings.RightClick;

        // Check left click activation
        if (leftSettings.Enabled && leftSettings.KeyType == "keyboard" && leftSettings.KeyCode == vkCode)
        {
            HandleActivation(isDown, leftSettings.Mode, ref _leftToggleState,
                () => StartLeftClicking(leftSettings.Cps), StopLeftClicking);
        }

        // Check right click activation
        if (rightSettings.Enabled && rightSettings.KeyType == "keyboard" && rightSettings.KeyCode == vkCode)
        {
            HandleActivation(isDown, rightSettings.Mode, ref _rightToggleState,
                () => StartRightClicking(rightSettings.Cps), StopRightClicking);
        }
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

    private void StartLeftClicking(int cps)
    {
        if (_leftClickActive) return;
        _leftClickActive = true;
        _leftClicker.Start(cps);
    }

    private void StopLeftClicking()
    {
        _leftClickActive = false;
        _leftClicker.Stop();
    }

    private void StartRightClicking(int cps)
    {
        if (_rightClickActive) return;
        _rightClickActive = true;
        _rightClicker.Start(cps);
    }

    private void StopRightClicking()
    {
        _rightClickActive = false;
        _rightClicker.Stop();
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
