using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DualAutoClicker.Native;

/// <summary>
/// High-precision clicker using spin-wait for accurate CPS
/// </summary>
public class PrecisionClicker : IDisposable
{
    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);

    private Thread? _thread;
    private volatile bool _running;
    private volatile int _intervalMicroseconds;
    private readonly Action _clickAction;
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

    public void Start(int cps)
    {
        if (_running) return;

        // Calculate interval in microseconds for better precision
        _intervalMicroseconds = (int)(1_000_000.0 / cps);
        _running = true;

        _thread = new Thread(ClickLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest
        };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(100);
        _thread = null;
    }

    private void ClickLoop()
    {
        var stopwatch = Stopwatch.StartNew();
        long nextClickTime = 0;

        while (_running)
        {
            long elapsed = stopwatch.ElapsedTicks;
            long elapsedMicroseconds = elapsed * 1_000_000 / Stopwatch.Frequency;

            if (elapsedMicroseconds >= nextClickTime)
            {
                _clickAction();
                nextClickTime += _intervalMicroseconds;

                // Prevent drift accumulation
                if (nextClickTime < elapsedMicroseconds)
                {
                    nextClickTime = elapsedMicroseconds + _intervalMicroseconds;
                }
            }
            else
            {
                // Short sleep to reduce CPU usage, then spin-wait for precision
                long remainingMs = (nextClickTime - elapsedMicroseconds) / 1000;
                if (remainingMs > 2)
                {
                    Thread.Sleep(1);
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
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
