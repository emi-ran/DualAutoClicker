using System.Runtime.InteropServices;

namespace DualAutoClicker.Native;

/// <summary>
/// High-resolution multimedia timer for precise CPS control
/// Uses winmm.dll for sub-millisecond precision
/// </summary>
public class MultimediaTimer : IDisposable
{
    private delegate void TimerCallback(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint timeSetEvent(uint uDelay, uint uResolution, TimerCallback lpTimeProc, UIntPtr dwUser, uint fuEvent);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint timeKillEvent(uint uTimerID);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint timeEndPeriod(uint uPeriod);

    private const uint TIME_PERIODIC = 1;
    private const uint TIME_KILL_SYNCHRONOUS = 0x0100;

    private uint _timerId;
    private TimerCallback? _callback;
    private bool _disposed;
    private static bool _periodSet;

    public event Action? Elapsed;

    /// <summary>
    /// Set system timer resolution to 1ms for all timers
    /// Call once at app start
    /// </summary>
    public static void SetHighResolution()
    {
        if (!_periodSet)
        {
            timeBeginPeriod(1);
            _periodSet = true;
        }
    }

    /// <summary>
    /// Reset system timer resolution
    /// Call once at app exit
    /// </summary>
    public static void ResetResolution()
    {
        if (_periodSet)
        {
            timeEndPeriod(1);
            _periodSet = false;
        }
    }

    /// <summary>
    /// Start the timer with specified interval in milliseconds
    /// </summary>
    public void Start(uint intervalMs)
    {
        Stop();

        // Keep callback reference to prevent GC
        _callback = OnTimerCallback;

        _timerId = timeSetEvent(
            intervalMs,
            1, // 1ms resolution
            _callback,
            UIntPtr.Zero,
            TIME_PERIODIC | TIME_KILL_SYNCHRONOUS
        );
    }

    /// <summary>
    /// Stop the timer
    /// </summary>
    public void Stop()
    {
        if (_timerId != 0)
        {
            timeKillEvent(_timerId);
            _timerId = 0;
        }
    }

    private void OnTimerCallback(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2)
    {
        Elapsed?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
