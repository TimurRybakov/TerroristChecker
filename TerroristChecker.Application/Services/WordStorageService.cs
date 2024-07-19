using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Application.Services;

/// <summary>
/// Stores all words in a unique set to reduce memory allocations.
/// </summary>
/// <param name="capacity">Predefined capacity o a word set.</param>
/// <param name="wordSeparators">Symbols to separate words in a multiwords strings passed to ParseWords method.</param>
public sealed class WordStorageService(int capacity, char[]? wordSeparators = null) : IWordStorageService
{
    private readonly HashSet<string> _words = new(capacity);

    private char[] WordSeparators { get; set; } = wordSeparators ?? [' ', '-'];

    /// <summary>
    /// Returns array of words parsed from am input.
    /// </summary>
    /// <param name="words">Input string.</param>
    /// <param name="prepareWord">Word prepare function.</param>
    /// <returns>Array where each element is a single not unique word.</returns>
    public WordModel[] ParseWords(string words, Func<string, string> prepareWord)
    {
        var wordsArray = words.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (wordsArray.Length > byte.MaxValue)
        {
            throw new ArithmeticException(
                $"Number of words ({words.Length}) in fullName exceeded maximum allowed number of {byte.MaxValue}");
        }

        var results = wordsArray
            .Select((unpreparedWord, i) => new WordModel(Value: prepareWord(unpreparedWord), Index: (byte)i))
            .OrderBy(w => w.Index)
            .ToArray();

        return results;
    }

    /// <summary>
    /// Tries to get the word from storage.
    /// </summary>
    /// <param name="word">The word to compare with.</param>
    /// <returns>Matched word from storage.</returns>
    public string? TryGet(string word)
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
        var value = TryGet(word);

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
