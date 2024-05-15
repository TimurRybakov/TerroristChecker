using TerroristChecker.Application.Abstractions.Cache;
using TerroristChecker.Application.Abstractions.Cqrs;
using TerroristChecker.Domain.Abstractions;
using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Application.Cqrs.Queries.SearchTerrorists;

public sealed record SearchTerroristsQuery(
    string FullName,
    int? Count = null,
    SearchOptions? SearchOptions = null) : ICachedQuery<List<SearchTerroristsQueryResponse>>
{
    public TimeSpan? Expiration { get; } = TimeSpan.FromMinutes(15);
}

internal sealed class SearchTerroristQueryHandler(IPersonSearcherService personSearcherService)
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
                    nameFull: x.Person.Key.FullName,
                    x.Person.Key.Birthday,
                    x.AvgCoefficient))
            .Take(request.Count ?? 1)
            .ToList();

        return Task.FromResult(result);
    }
}
