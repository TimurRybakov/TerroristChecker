using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Domain.Dice.Abstractions;

public interface IPersonSearcherService
{
    void Add(int id, string fullName, DateOnly? birthday);

    void Clear();

    Task<(KeyValuePair<PersonModel,NamesSearchResultModel> Person, double AvgCoefficient)[]?> SearchAsync(
        string input, SearchOptions? searchOptions, CancellationToken cancellationToken);
}
