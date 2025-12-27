using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DualAutoClicker.Native;

/// <summary>
/// Global low-level mouse hook to detect mouse button presses
/// </summary>
public class MouseHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_XBUTTONUP = 0x020C;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int XBUTTON1 = 0x0001; // MB4
    private const int XBUTTON2 = 0x0002; // MB5

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public int X;
        public int Y;
        public int MouseData;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;

    public event Action<bool>? XButton1StateChanged; // MB4
    public event Action<bool>? XButton2StateChanged; // MB5
    public event Action<bool>? MiddleButtonStateChanged; // MB3

    /// <summary>
    /// Fired when any extra mouse button is pressed (for key binding capture)
    /// Parameters: (buttonCode, buttonName)
    /// </summary>
    public event Action<int, string>? MouseButtonPressed;

    public MouseHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
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
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            int xButton = (hookStruct.MouseData >> 16) & 0xFFFF;

            if (wParam == WM_XBUTTONDOWN)
            {
                if (xButton == XBUTTON1)
                {
                    XButton1StateChanged?.Invoke(true);
                    MouseButtonPressed?.Invoke(4, "MB4");
                }
                else if (xButton == XBUTTON2)
                {
                    XButton2StateChanged?.Invoke(true);
                    MouseButtonPressed?.Invoke(5, "MB5");
                }
            }
            else if (wParam == WM_XBUTTONUP)
            {
                if (xButton == XBUTTON1) XButton1StateChanged?.Invoke(false);
                else if (xButton == XBUTTON2) XButton2StateChanged?.Invoke(false);
            }
            else if (wParam == WM_MBUTTONDOWN)
            {
                MiddleButtonStateChanged?.Invoke(true);
                MouseButtonPressed?.Invoke(3, "MB3");
            }
            else if (wParam == WM_MBUTTONUP)
            {
                MiddleButtonStateChanged?.Invoke(false);
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public static string GetButtonName(int buttonCode)
    {
        return buttonCode switch
        {
            3 => "MB3",
            4 => "MB4",
            5 => "MB5",
            _ => $"MB{buttonCode}"
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
