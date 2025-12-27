using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DualAutoClicker.Native;

/// <summary>
/// Global low-level keyboard hook to detect any key presses
/// </summary>
public class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int VkCode;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;

    /// <summary>
    /// Fired when any key state changes. Parameters: (VK code, isDown)
    /// </summary>
    public event Action<int, bool>? KeyStateChanged;

    /// <summary>
    /// Fired when any key is pressed (for key binding capture)
    /// </summary>
    public event Action<int, string>? KeyPressed;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int vkCode = hookStruct.VkCode;

            bool isDown = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
            bool isUp = wParam == WM_KEYUP || wParam == WM_SYSKEYUP;

            if (isDown)
            {
                KeyPressed?.Invoke(vkCode, GetKeyName(vkCode));
            }

            if (isDown || isUp)
            {
                KeyStateChanged?.Invoke(vkCode, isDown);
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public static string GetKeyName(int vkCode)
    {
        return vkCode switch
        {
            0x70 => "F1",
            0x71 => "F2",
            0x72 => "F3",
            0x73 => "F4",
            0x74 => "F5",
            0x75 => "F6",
            0x76 => "F7",
            0x77 => "F8",
            0x78 => "F9",
            0x79 => "F10",
            0x7A => "F11",
            0x7B => "F12",
            0x1B => "ESC",
            0x09 => "TAB",
            0x14 => "CAPS",
            0x10 => "SHIFT",
            0x11 => "CTRL",
            0x12 => "ALT",
            0x20 => "SPACE",
            0x0D => "ENTER",
            0x08 => "BACKSPACE",
            0x2D => "INSERT",
            0x2E => "DELETE",
            0x24 => "HOME",
            0x23 => "END",
            0x21 => "PGUP",
            0x22 => "PGDN",
            0x25 => "LEFT",
            0x26 => "UP",
            0x27 => "RIGHT",
            0x28 => "DOWN",
            >= 0x30 and <= 0x39 => ((char)vkCode).ToString(), // 0-9
            >= 0x41 and <= 0x5A => ((char)vkCode).ToString(), // A-Z
            >= 0x60 and <= 0x69 => $"NUM{vkCode - 0x60}",     // Numpad 0-9
            0xC0 => "`",
            0xBD => "-",
            0xBB => "=",
            0xDB => "[",
            0xDD => "]",
            0xDC => "\\",
            0xBA => ";",
            0xDE => "'",
            0xBC => ",",
            0xBE => ".",
            0xBF => "/",
            _ => $"KEY_{vkCode}"
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        Uninstall();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
