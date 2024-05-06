namespace TerroristChecker.Application.Cqrs.Queries.SearchTerrorists;

public sealed class SearchTerroristsQueryResponse
{
    public int? Id { get; init; }
    public string? NameFull { get; init; }

    public DateOnly? Birthday { get; init; }

    public double? Coefficient { get; init; }

    public SearchTerroristsQueryResponse(int id, string nameFull, DateOnly? birthday, double coefficient)
    {
        Id = id;
        NameFull = nameFull;
        Birthday = birthday;
        Coefficient = coefficient;
    }

    public SearchTerroristsQueryResponse()
    {

    }
}
