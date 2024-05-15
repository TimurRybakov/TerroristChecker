using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Domain.Dice.Abstractions;

public interface IPersonSearcherService
{
    void Add(int id, string fullName, DateOnly? birthday);

    void Clear();

    (KeyValuePair<PersonModel,NamesSearchResultModel> Person, double AvgCoefficient)[]? Search(
        string input, SearchOptions? searchOptions);
}
