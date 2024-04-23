using TerroristChecker.Domain.Dice.Abstractions;

namespace TerroristChecker.Application.Dice.Services;

/// <summary>
/// Stores all words in a unique set to reduce memory allocations.
/// </summary>
/// <param name="capacity">Predefined capacity o a word set.</param>
/// <param name="wordSeparators">Symbols to separate words in a multiwords strings passed to ParseWords method.</param>
public sealed class WordStorageService(int capacity, char[]? wordSeparators = null) : IWordStorageService
{
    private readonly HashSet<string> _words = new HashSet<string>(capacity);

    private char[] WordSeparators { get; set; } = wordSeparators ?? [' ', '-'];

    /// <summary>
    /// Returns array of words parsed from am input.
    /// </summary>
    /// <param name="words">Input string.</param>
    /// <returns>Array where each element is a single not unique word.</returns>
    public string[] ParseWords(string words)
    {
        return words.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Tries to get the word from storage.
    /// </summary>
    /// <param name="word">The word to compare with.</param>
    /// <returns>Matched word from storage.</returns>
    public string? Get(string word)
    {
        return _words.TryGetValue(word, out var value) ? value : null;
    }

    /// <summary>
    /// Gets exactly the matched word from storage or adds a new one returning it.
    /// </summary>
    /// <param name="word">The word to add or compare with.</param>
    /// <returns>Matched or added word from storage</returns>
    public string GetOrAdd(string word)
    {
        var value = Get(word);

        if (value is not null)
        {
            return value;
        }

        _words.Add(word);

        return word;
    }

    /// <summary>
    /// Clears words hashset.
    /// </summary>
    public void Clear()
    {
        _words.Clear();
    }
}
