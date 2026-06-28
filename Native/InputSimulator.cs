using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DualAutoClicker.Native;

/// <summary>
/// Simulates mouse clicks and keyboard input using SendInput for reliable operation
/// </summary>
public static class InputSimulator
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

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
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT
    {
        [FieldOffset(0)]
        public uint type;
        
        // On 64-bit, the union starts at offset 8 due to alignment
        [FieldOffset(8)]
        public MOUSEINPUT mi;
        [FieldOffset(8)]
        public KEYBDINPUT ki;
        [FieldOffset(8)]
        public HARDWAREINPUT hi;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    // Static fields for window targeting
    public static bool WindowTargetEnabled { get; set; }
    public static string TargetProcessName { get; set; } = "";
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
        if (!string.IsNullOrWhiteSpace(TargetProcessName))
        {
            GetWindowThreadProcessId(foregroundWindow, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                var allowedProcesses = TargetProcessName
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (allowedProcesses.Length > 0 &&
                    !allowedProcesses.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Target process check failed: {ex}");
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

    /// <summary>
    /// Simulate typing text using Unicode input (works with any character)
    /// </summary>
    public static void SendText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Don't type if our app is in foreground
        if (IsOurAppInForeground()) return;

        // Don't type if target window is not in foreground
        if (!IsTargetWindowInForeground()) return;

        // Create input array: 2 events per character (down + up)
        var inputs = new INPUT[text.Length * 2];

        for (int i = 0; i < text.Length; i++)
        {
            ushort scanCode = text[i];

            // Key down
            inputs[i * 2].type = INPUT_KEYBOARD;
            inputs[i * 2].ki.wVk = 0;
            inputs[i * 2].ki.wScan = scanCode;
            inputs[i * 2].ki.dwFlags = KEYEVENTF_UNICODE;
            inputs[i * 2].ki.time = 0;
            inputs[i * 2].ki.dwExtraInfo = IntPtr.Zero;

            // Key up
            inputs[i * 2 + 1].type = INPUT_KEYBOARD;
            inputs[i * 2 + 1].ki.wVk = 0;
            inputs[i * 2 + 1].ki.wScan = scanCode;
            inputs[i * 2 + 1].ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            inputs[i * 2 + 1].ki.time = 0;
            inputs[i * 2 + 1].ki.dwExtraInfo = IntPtr.Zero;
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
