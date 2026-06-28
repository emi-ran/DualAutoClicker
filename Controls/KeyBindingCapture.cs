using DualAutoClicker.Native;

namespace DualAutoClicker.Controls;

internal sealed class KeyBindingCapture : IDisposable
{
    private MouseHook? _mouseHook;
    private KeyboardHook? _keyboardHook;
    private Action<int, string>? _mousePressedHandler;
    private Action<int, string>? _keyPressedHandler;
    private bool _disposed;

    public bool IsActive { get; private set; }
    public KeyboardHook? KeyboardHook => _keyboardHook;

    public void Start(Action<int, string> onMousePressed, Action<int, string> onKeyPressed)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsActive)
        {
            return;
        }

        _mouseHook = new MouseHook();
        _keyboardHook = new KeyboardHook();
        _mousePressedHandler = onMousePressed;
        _keyPressedHandler = onKeyPressed;

        _mouseHook.MouseButtonPressed += HandleMousePressed;
        _keyboardHook.KeyPressed += HandleKeyPressed;

        _mouseHook.Install();
        _keyboardHook.Install();

        IsActive = true;
    }

    public void Stop()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;

        _mouseHook?.Uninstall();
        _keyboardHook?.Uninstall();

        if (_mouseHook != null)
        {
            _mouseHook.MouseButtonPressed -= HandleMousePressed;
            _mouseHook.Dispose();
            _mouseHook = null;
        }

        if (_keyboardHook != null)
        {
            _keyboardHook.KeyPressed -= HandleKeyPressed;
            _keyboardHook.Dispose();
            _keyboardHook = null;
        }

        _mousePressedHandler = null;
        _keyPressedHandler = null;
    }

    private void HandleMousePressed(int code, string name)
    {
        _mousePressedHandler?.Invoke(code, name);
    }

    private void HandleKeyPressed(int code, string name)
    {
        _keyPressedHandler?.Invoke(code, name);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }
}
