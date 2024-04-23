namespace TerroristChecker.Domain.Dice.Abstractions;

public interface IWordStorageService
{
    string[] ParseWords(string words);

    string GetOrAdd(string word);

    void Clear();
}
