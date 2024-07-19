using TerroristChecker.Application.Abstractions.Cqrs;
using TerroristChecker.Domain.Abstractions;
using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Application.Cqrs.Queries.SearchTerrorists;

internal sealed class SearchTerroristsQueryHandler(IPersonSearcherService personSearcherService)
    : IQueryHandler<SearchTerroristsQuery, List<SearchTerroristsQueryResponse>>
{
    public Task<Result<List<SearchTerroristsQueryResponse>>> Handle(
        SearchTerroristsQuery request,
        CancellationToken cancellationToken)
    {
        var results = personSearcherService.Search(
            request.FullName, request.SearchOptions ?? SearchOptions.Default);

        Result<List<SearchTerroristsQueryResponse>> result = results is null
            ? new List<SearchTerroristsQueryResponse>()
            : results
            .Select(
                x => new SearchTerroristsQueryResponse(
                    x.Person.Key.Id,
                    x.Person.Key.FullName,
                    x.Person.Key.Birthday,
                    x.AvgCoefficient))
            .Take(request.Count ?? 1)
            .ToList();

        return Task.FromResult(result);
    }
}
