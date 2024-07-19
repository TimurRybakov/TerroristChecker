namespace TerroristChecker.Application.Cqrs.Queries.SearchTerrorists;

public sealed record SearchTerroristsQueryResponse(int Id, string NameFull, DateOnly? Birthday, double Coefficient);
