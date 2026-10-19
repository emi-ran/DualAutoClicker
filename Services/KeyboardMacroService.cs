using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using DualAutoClicker.Models;
using DualAutoClicker.Native;

namespace DualAutoClicker.Services;

/// <summary>
/// Service that generates anti-detection random text sequences and injects them
/// from a dedicated worker thread so the low-level keyboard hook never blocks.
/// Implements a "last 5 exclusion" rule: a character cannot be reused until 5 other characters have been used.
/// </summary>
public sealed class KeyboardMacroService : IDisposable
{
    private const int ExclusionCount = 5;
    private const int MaxJunkCharacterCount = 64;
    private const int MaxRandomCharCount = 50;
    private const int QueueCapacity = 2;

    /// <summary>
    /// Delay between the hotkey press and the injection. The hook callback returns
    /// immediately, so the physical key (and its modifiers) can be released first.
    /// </summary>
    private const int TriggerDelayMs = 30;

    private readonly record struct MacroJob(
        string BaseText,
        string JunkCharacters,
        int MinRandomChars,
        int MaxRandomChars);

    private readonly Random _random = new();
    private readonly Queue<char> _recentlyUsed = new();
    private readonly BlockingCollection<MacroJob> _queue = new(new ConcurrentQueue<MacroJob>(), QueueCapacity);
    private readonly CancellationTokenSource _cts = new();
    private readonly object _startLock = new();
    private Thread? _worker;

    private bool _disposed;

    public KeyboardMacroService()
    {
    }

    /// <summary>
    /// Starts the injection worker on first use so application startup does not
    /// pay for a thread that may never be needed.
    /// </summary>
    private void EnsureWorkerStarted()
    {
        if (_worker != null) return;

        lock (_startLock)
        {
            if (_worker != null) return;

            var worker = new Thread(ProcessQueue)
            {
                IsBackground = true,
                Name = "KeyboardMacroWorker",
                Priority = ThreadPriority.AboveNormal
            };

            _worker = worker;
            worker.Start();
        }
    }

    /// <summary>
    /// Queues a macro run. Called from the low-level hook thread, so it never blocks.
    /// When the queue is full the newest trigger wins.
    /// </summary>
    public void EnqueueMacro(KeyboardMacroSettings settings)
    {
        if (_disposed || !settings.Enabled) return;
        if (string.IsNullOrEmpty(settings.BaseText)) return;

        MacroJob job = CreateJob(settings);

        if (_queue.IsAddingCompleted) return;

        EnsureWorkerStarted();

        if (!_queue.TryAdd(job))
        {
            _queue.TryTake(out _);
            _queue.TryAdd(job);
        }
    }

    /// <summary>
    /// Clamps user input so a bad configuration can never flood the input queue.
    /// </summary>
    private static MacroJob CreateJob(KeyboardMacroSettings settings)
    {
        string baseText = settings.BaseText ?? string.Empty;
        if (baseText.Length > InputSimulator.MaxTextLength)
        {
            baseText = baseText[..InputSimulator.MaxTextLength];
        }

        int minRandom = Math.Clamp(settings.MinRandomChars, 0, MaxRandomCharCount);
        int maxRandom = Math.Clamp(settings.MaxRandomChars, minRandom, MaxRandomCharCount);

        string junkCharacters = settings.JunkCharacters ?? string.Empty;
        if (junkCharacters.Length > MaxJunkCharacterCount)
        {
            junkCharacters = junkCharacters[..MaxJunkCharacterCount];
        }

        return new MacroJob(baseText, junkCharacters, minRandom, maxRandom);
    }

    private void ProcessQueue()
    {
        try
        {
            foreach (MacroJob job in _queue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    // Let the physical hotkey and its modifiers be released first.
                    if (_cts.Token.WaitHandle.WaitOne(TriggerDelayMs))
                    {
                        return;
                    }

                    string text = GenerateText(job);
                    if (text.Length == 0) continue;

                    if (!InputSimulator.SendText(text, _cts.Token))
                    {
                        Debug.WriteLine("Macro text was not injected (focus changed or SendInput failed).");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Macro execution failed: {ex}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
    }

    /// <summary>
    /// Generates the macro output: base text + random junk characters
    /// </summary>
    private string GenerateText(MacroJob job)
    {
        if (string.IsNullOrEmpty(job.BaseText))
            return string.Empty;

        if (string.IsNullOrEmpty(job.JunkCharacters))
            return job.BaseText;

        int count = _random.Next(job.MinRandomChars, job.MaxRandomChars + 1);
        if (count <= 0)
            return job.BaseText;

        // Reserve one character for the separating space
        int availableBudget = InputSimulator.MaxTextLength - job.BaseText.Length - 1;
        if (availableBudget <= 0)
            return job.BaseText;

        count = Math.Min(count, availableBudget);

        var result = new StringBuilder(job.BaseText, job.BaseText.Length + count + 1);
        result.Append(' '); // Space before junk

        for (int i = 0; i < count; i++)
        {
            result.Append(GetNextRandomChar(job.JunkCharacters));
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets the next random character, excluding recently used ones
    /// </summary>
    private char GetNextRandomChar(string availableChars)
    {
        // Build list of allowed characters (exclude recently used)
        var allowed = new List<char>(availableChars.Length);
        foreach (char c in availableChars)
        {
            if (!_recentlyUsed.Contains(c))
            {
                allowed.Add(c);
            }
        }

        // If all chars are excluded, use any char (shouldn't happen with proper config)
        if (allowed.Count == 0)
        {
            allowed.AddRange(availableChars);
        }

        // Pick random from allowed
        char selected = allowed[_random.Next(allowed.Count)];

        // Add to recently used queue
        _recentlyUsed.Enqueue(selected);

        // Maintain queue size
        while (_recentlyUsed.Count > ExclusionCount)
        {
            _recentlyUsed.Dequeue();
        }

        return selected;
    }

    /// <summary>
    /// Resets the recently used character tracking
    /// </summary>
    public void Reset()
    {
        _recentlyUsed.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _queue.CompleteAdding();
        _cts.Cancel();

        Thread? worker = _worker;
        if (worker == null)
        {
            _queue.Dispose();
            _cts.Dispose();
            return;
        }

        // Only release the queue when the worker has stopped; otherwise the background
        // thread could touch disposed objects while the process is shutting down.
        if (worker.Join(500))
        {
            _queue.Dispose();
            _cts.Dispose();
        }
    }
}
