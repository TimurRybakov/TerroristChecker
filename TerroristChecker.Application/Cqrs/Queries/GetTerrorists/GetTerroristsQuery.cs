using TerroristChecker.Application.Abstractions.Cqrs;
using TerroristChecker.Domain.Dice.Entities;

namespace TerroristChecker.Application.Cqrs.Queries.GetTerrorists;

public sealed record GetTerroristsQuery : IQuery<List<Person>>;
