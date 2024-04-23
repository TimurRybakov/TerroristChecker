using TerroristChecker.Application.Abstractions;
using TerroristChecker.Domain.Abstractions;
using TerroristChecker.Domain.Dice.Abstractions;

namespace TerroristChecker.Application.Cqrs.Queries;

public sealed record SearchTerroristsQuery(
    string FullName,
    int? Count = null,
    SearchOptions? SearchOptions = null) : IQuery<IList<SearchTerroristResponse>>;

internal sealed class SearchTerroristQueryHandler(IPersonCacheService personCacheService)
    : IQueryHandler<SearchTerroristsQuery, IList<SearchTerroristResponse>>
{
    public async Task<Result<IList<SearchTerroristResponse>>> Handle(
        SearchTerroristsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await personCacheService.SearchAsync(
            request.FullName, request.SearchOptions ?? SearchOptions.Default);

        if (results is null)
        {
            return new List<SearchTerroristResponse>();
        }

        return results
            .Select(
                x => new SearchTerroristResponse(
                    x.Key.Id,
                    nameFull: string.Join(" ", x.Key.Names),
                    x.Key.Birthday,
                    x.Value.AvgCoefficient))
            .Take(request.Count ?? 1)
            .ToList();
    }
}
