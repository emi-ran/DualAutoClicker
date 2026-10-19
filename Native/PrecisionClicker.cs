using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DualAutoClicker.Native;

/// <summary>
/// High-precision clicker using spin-wait for accurate CPS with optional randomization
/// </summary>
public class PrecisionClicker : IDisposable
{
    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);

    /// <summary>
    /// Per-run stop flag. A dedicated token lets a new run start immediately while the
    /// previous thread is still finishing its last click (no shared flag race).
    /// </summary>
    private sealed class RunToken
    {
        public volatile bool Running = true;
    }

    private RunToken? _runToken;
    private Thread? _thread;
    private volatile int _baseCps;
    private volatile int _randomPercent;
    private readonly Action _clickAction;
    private readonly Random _random = new();
    private bool _disposed;
    private static bool _periodSet;

    public PrecisionClicker(Action clickAction)
    {
        _clickAction = clickAction;
    }

    public static void SetHighResolution()
    {
        if (!_periodSet)
        {
            timeBeginPeriod(1);
            _periodSet = true;
        }
    }

    public static void ResetResolution()
    {
        if (_periodSet)
        {
            timeEndPeriod(1);
            _periodSet = false;
        }
    }

    public void Start(int cps, int randomPercent = 0)
    {
        if (_runToken is { Running: true }) return;

        _baseCps = cps;
        _randomPercent = randomPercent;

        var token = new RunToken();
        _runToken = token;

        // AboveNormal keeps the UI thread responsive; Highest could starve the
        // rest of the system while spin-waiting.
        _thread = new Thread(() => ClickLoop(token))
        {
            IsBackground = true,
            Name = "PrecisionClicker",
            Priority = ThreadPriority.AboveNormal
        };
        _thread.Start();
    }

    /// <summary>
    /// Stops clicking without blocking. This is called from the low-level hook callback,
    /// so joining the worker thread here would freeze input for the whole system.
    /// </summary>
    public void Stop()
    {
        RunToken? token = Interlocked.Exchange(ref _runToken, null);
        if (token == null) return;

        token.Running = false;
    }

    /// <summary>
    /// Stops clicking and waits for the worker thread to finish. Only for shutdown paths,
    /// never for the hook callback.
    /// </summary>
    public void StopAndWait(int timeoutMs)
    {
        Stop();

        Thread? thread = _thread;
        if (thread != null && thread.IsAlive)
        {
            thread.Join(timeoutMs);
        }

        _thread = null;
    }

    private void ClickLoop(RunToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        long nextClickTime = 0;

        while (token.Running)
        {
            long elapsed = stopwatch.ElapsedTicks;
            long elapsedMicroseconds = elapsed * 1_000_000 / Stopwatch.Frequency;

            if (elapsedMicroseconds >= nextClickTime)
            {
                _clickAction();

                // Calculate next interval with optional randomization
                int effectiveCps = _baseCps;
                if (_randomPercent > 0)
                {
                    double variance = _baseCps * _randomPercent / 100.0;
                    effectiveCps = (int)(_baseCps + (_random.NextDouble() * 2 - 1) * variance);
                    effectiveCps = Math.Max(1, effectiveCps);
                }

                int intervalMicroseconds = (int)(1_000_000.0 / effectiveCps);
                nextClickTime += intervalMicroseconds;

                // Prevent drift accumulation
                if (nextClickTime < elapsedMicroseconds)
                {
                    nextClickTime = elapsedMicroseconds + intervalMicroseconds;
                }
            }
            else
            {
                // Sleep for most of the remaining interval, then spin only for the
                // last couple of milliseconds. Sleeping 1 ms at a time woke the CPU
                // hundreds of times per second for no timing benefit.
                long remainingMs = (nextClickTime - elapsedMicroseconds) / 1000;
                if (remainingMs > 2)
                {
                    Thread.Sleep((int)(remainingMs - 2));
                }
                else
                {
                    Thread.SpinWait(10);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        // Wait for the worker so no click is injected while the app is going away.
        StopAndWait(500);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
