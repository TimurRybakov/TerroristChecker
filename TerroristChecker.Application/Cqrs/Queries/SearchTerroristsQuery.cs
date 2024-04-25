using TerroristChecker.Application.Abstractions;
using TerroristChecker.Domain.Abstractions;
using TerroristChecker.Domain.Dice.Abstractions;

namespace TerroristChecker.Application.Cqrs.Queries;

public sealed record SearchTerroristsQuery(
    string FullName,
    int? Count = null,
    SearchOptions? SearchOptions = null) : IQuery<IList<SearchTerroristResponse>>;

internal sealed class SearchTerroristQueryHandler(IPersonSearcherService personSearcherService)
    : IQueryHandler<SearchTerroristsQuery, IList<SearchTerroristResponse>>
{
    public async Task<Result<IList<SearchTerroristResponse>>> Handle(
        SearchTerroristsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await personSearcherService.SearchAsync(
            request.FullName, request.SearchOptions ?? SearchOptions.Default, cancellationToken);

        if (results is null)
        {
            return new List<SearchTerroristResponse>();
        }

        return results
            .Select(
                x => new SearchTerroristResponse(
                    x.Person.Key.Id,
                    nameFull: x.Person.Key.FullName,
                    x.Person.Key.Birthday,
                    x.AvgCoefficient))
            .Take(request.Count ?? 1)
            .ToList();
    }
}
