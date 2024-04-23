using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Domain.Dice.Abstractions;

public interface IPersonCacheService
{
    void Add(int id, string fullName, DateOnly? birthday);

    void Clear();

    Task<KeyValuePair<PersonModel, SearchResultModel>[]?> SearchAsync(string input, SearchOptions? searchOptions);
}
