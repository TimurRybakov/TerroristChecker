using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Domain.Dice.Abstractions;

public interface IWordStorageService
{
    WordModel[] ParseWords(string words, Func<string, string> prepareWord);

    string GetOrAdd(string word);

    void Clear();
}
