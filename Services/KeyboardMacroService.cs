using DualAutoClicker.Models;
using DualAutoClicker.Native;

namespace DualAutoClicker.Services;

/// <summary>
/// Service that generates anti-detection random text sequences.
/// Implements a "last 5 exclusion" rule: a character cannot be reused until 5 other characters have been used.
/// </summary>
public class KeyboardMacroService
{
    private readonly Random _random = new();
    private readonly Queue<char> _recentlyUsed = new();
    private const int ExclusionCount = 5;

    /// <summary>
    /// Generates the macro output: base text + random junk characters
    /// </summary>
    /// <param name="settings">The macro settings</param>
    /// <returns>The complete text to send</returns>
    public string GenerateMacroText(KeyboardMacroSettings settings)
    {
        if (string.IsNullOrEmpty(settings.BaseText))
            return string.Empty;

        var junkChars = settings.JunkCharacters;
        if (string.IsNullOrEmpty(junkChars))
            return settings.BaseText;

        // Determine how many random chars to append
        int count = _random.Next(settings.MinRandomChars, settings.MaxRandomChars + 1);

        var result = new System.Text.StringBuilder(settings.BaseText);
        result.Append(' '); // Space before junk

        for (int i = 0; i < count; i++)
        {
            char nextChar = GetNextRandomChar(junkChars);
            result.Append(nextChar);
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets the next random character, excluding recently used ones
    /// </summary>
    private char GetNextRandomChar(string availableChars)
    {
        // Build list of allowed characters (exclude recently used)
        var allowed = new List<char>();
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
    /// Executes the macro: generates text and sends it via keyboard simulation
    /// </summary>
    public void ExecuteMacro(KeyboardMacroSettings settings)
    {
        string text = GenerateMacroText(settings);
        if (!string.IsNullOrEmpty(text))
        {
            InputSimulator.SendText(text);
        }
    }

    /// <summary>
    /// Resets the recently used character tracking
    /// </summary>
    public void Reset()
    {
        _recentlyUsed.Clear();
    }
}
