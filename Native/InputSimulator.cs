using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    /// <summary>
    /// Maximum number of characters that may be injected for a single macro run.
    /// Prevents flooding the system input queue.
    /// </summary>
    public const int MaxTextLength = 200;

    // Injecting hundreds of events in one SendInput call overflows the OS input queue,
    // which makes Windows drop events and play its default beep. Send small chunks with
    // a short pause instead so every event is consumed by the target application.
    private const int CharsPerBatch = 4;
    private const int BatchDelayMs = 8;

    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    // Process name lookups are cached by PID; a Process object per click is far too expensive.
    private static readonly object ProcessNameCacheLock = new();
    private static readonly TimeSpan ProcessNameCacheTtl = TimeSpan.FromSeconds(2);
    private static uint _cachedPid;
    private static string? _cachedProcessName;
    private static long _cachedProcessNameTimestamp;

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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags,
        StringBuilder lpExeName, ref uint lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

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

            string? processName = GetProcessName(processId);
            if (processName == null) return false;

            var allowedProcesses = TargetProcessName
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (allowedProcesses.Length > 0 &&
                !allowedProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Resolves a process name without allocating a <see cref="Process"/> object.
    /// The last lookup is cached per PID for a short time because this runs on every click.
    /// </summary>
    private static string? GetProcessName(uint processId)
    {
        if (processId == 0) return null;

        lock (ProcessNameCacheLock)
        {
            long now = Environment.TickCount64;
            if (_cachedPid == processId &&
                _cachedProcessName != null &&
                now - _cachedProcessNameTimestamp < ProcessNameCacheTtl.TotalMilliseconds)
            {
                return _cachedProcessName;
            }

            string? name = QueryProcessName(processId);
            _cachedPid = processId;
            _cachedProcessName = name;
            _cachedProcessNameTimestamp = now;
            return name;
        }
    }

    private static string? QueryProcessName(uint processId)
    {
        IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (handle == IntPtr.Zero) return null;

        try
        {
            var buffer = new StringBuilder(1024);
            uint size = (uint)buffer.Capacity;
            if (!QueryFullProcessImageName(handle, 0, buffer, ref size))
            {
                Debug.WriteLine($"QueryFullProcessImageName failed: {Marshal.GetLastWin32Error()}");
                return null;
            }

            string? fileName = Path.GetFileNameWithoutExtension(buffer.ToString());
            return string.IsNullOrEmpty(fileName) ? null : fileName;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Process name lookup failed: {ex}");
            return null;
        }
        finally
        {
            CloseHandle(handle);
        }
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

        SendInput(2, inputs, InputSize);
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

        SendInput(2, inputs, InputSize);
    }

    /// <summary>
    /// Simulate typing text using Unicode input (works with any character).
    /// Text is injected in small batches so the system input queue never overflows.
    /// </summary>
    /// <returns>True when the whole text was injected, false when it was skipped or failed.</returns>
    public static bool SendText(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // Don't type if our app is in foreground
        if (IsOurAppInForeground()) return false;

        // Don't type if target window is not in foreground
        if (!IsTargetWindowInForeground()) return false;

        int totalLength = text.Length;
        if (totalLength > MaxTextLength)
        {
            Debug.WriteLine($"Macro text truncated from {totalLength} to {MaxTextLength} characters.");
            totalLength = MaxTextLength;
        }

        var inputs = new INPUT[CharsPerBatch * 2];
        int sent = 0;

        while (sent < totalLength)
        {
            if (cancellationToken.IsCancellationRequested) return false;

            int count = Math.Min(CharsPerBatch, totalLength - sent);

            for (int i = 0; i < count; i++)
            {
                ushort scanCode = text[sent + i];

                // Key down
                inputs[i * 2].type = INPUT_KEYBOARD;
                inputs[i * 2].ki.wVk = 0;
                inputs[i * 2].ki.wScan = scanCode;
                inputs[i * 2].ki.dwFlags = KEYEVENTF_UNICODE;

                // Key up
                inputs[i * 2 + 1].type = INPUT_KEYBOARD;
                inputs[i * 2 + 1].ki.wVk = 0;
                inputs[i * 2 + 1].ki.wScan = scanCode;
                inputs[i * 2 + 1].ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            }

            uint injected = SendInput((uint)(count * 2), inputs, InputSize);
            if (injected == 0)
            {
                Debug.WriteLine($"SendInput(text) failed: {Marshal.GetLastWin32Error()}");
                return false;
            }

            sent += count;

            if (sent < totalLength)
            {
                // Give the target application time to consume the batch. Without this the
                // input queue can overflow and Windows answers with its default beep.
                if (cancellationToken.WaitHandle.WaitOne(BatchDelayMs)) return false;

                // Abort if focus moved away while typing
                if (IsOurAppInForeground() || !IsTargetWindowInForeground())
                {
                    Debug.WriteLine("Macro text aborted: foreground window changed.");
                    return false;
                }
            }
        }

        return true;
    }
}
