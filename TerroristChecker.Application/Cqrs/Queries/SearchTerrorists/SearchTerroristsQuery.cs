using TerroristChecker.Application.Abstractions.Cache;
using TerroristChecker.Application.Abstractions.Cqrs;
using TerroristChecker.Domain.Abstractions;
using TerroristChecker.Domain.Dice.Abstractions;

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
    public async Task<Result<List<SearchTerroristsQueryResponse>>> Handle(
        SearchTerroristsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await personSearcherService.SearchAsync(
            request.FullName, request.SearchOptions ?? SearchOptions.Default, cancellationToken);

        if (results is null)
        {
            return new List<SearchTerroristsQueryResponse>();
        }

        return results
            .Select(
                x => new SearchTerroristsQueryResponse(
                    x.Person.Key.Id,
                    nameFull: x.Person.Key.FullName,
                    x.Person.Key.Birthday,
                    x.AvgCoefficient))
            .Take(request.Count ?? 1)
            .ToList();
    }
}
