using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DualAutoClicker.Native;

/// <summary>
/// Simulates mouse clicks using SendInput for reliable operation
/// </summary>
public static class InputSimulator
{
    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public MOUSEINPUT mi;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    // Static fields for window targeting
    public static bool WindowTargetEnabled { get; set; }
    public static string TargetProcessName { get; set; } = "";
    public static string TargetWindowTitle { get; set; } = "";

    /// <summary>
    /// Check if our application window is in foreground
    /// </summary>
    public static bool IsOurAppInForeground()
    {
        var foregroundWindow = GetForegroundWindow();
        GetWindowThreadProcessId(foregroundWindow, out uint foregroundProcessId);
        return foregroundProcessId == Environment.ProcessId;
    }

    /// <summary>
    /// Check if the target window is in foreground (when window targeting is enabled)
    /// </summary>
    public static bool IsTargetWindowInForeground()
    {
        if (!WindowTargetEnabled) return true;

        var foregroundWindow = GetForegroundWindow();

        // Check by process name
        if (!string.IsNullOrEmpty(TargetProcessName))
        {
            GetWindowThreadProcessId(foregroundWindow, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                if (!process.ProcessName.Equals(TargetProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        // Check by window title
        if (!string.IsNullOrEmpty(TargetWindowTitle))
        {
            var sb = new System.Text.StringBuilder(256);
            GetWindowText(foregroundWindow, sb, sb.Capacity);
            if (!sb.ToString().Contains(TargetWindowTitle, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Simulate a left mouse click (down + up)
    /// </summary>
    public static void LeftClick()
    {
        // Don't click if our app is in foreground
        if (IsOurAppInForeground()) return;

        // Don't click if target window is not in foreground
        if (!IsTargetWindowInForeground()) return;

        var inputs = new INPUT[2];

        // Mouse down
        inputs[0].type = INPUT_MOUSE;
        inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

        // Mouse up
        inputs[1].type = INPUT_MOUSE;
        inputs[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    /// <summary>
    /// Simulate a right mouse click (down + up)
    /// </summary>
    public static void RightClick()
    {
        // Don't click if our app is in foreground
        if (IsOurAppInForeground()) return;

        // Don't click if target window is not in foreground
        if (!IsTargetWindowInForeground()) return;

        var inputs = new INPUT[2];

        // Mouse down
        inputs[0].type = INPUT_MOUSE;
        inputs[0].mi.dwFlags = MOUSEEVENTF_RIGHTDOWN;

        // Mouse up
        inputs[1].type = INPUT_MOUSE;
        inputs[1].mi.dwFlags = MOUSEEVENTF_RIGHTUP;

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }
}
