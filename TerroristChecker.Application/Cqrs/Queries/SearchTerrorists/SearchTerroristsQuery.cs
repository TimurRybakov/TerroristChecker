using TerroristChecker.Application.Abstractions.Cache;
using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Application.Cqrs.Queries.SearchTerrorists;

public sealed record SearchTerroristsQuery(
    string FullName,
    int? Count = null,
    SearchOptions? SearchOptions = null) : ICachedQuery<List<SearchTerroristsQueryResponse>>
{
    public TimeSpan? Expiration { get; } = TimeSpan.FromMinutes(15);
}
