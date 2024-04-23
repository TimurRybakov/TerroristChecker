using TerroristChecker.Domain.Abstractions;

namespace TerroristChecker.Domain.Dice.Entities;

public sealed class Person : Entity
{
    public string FullName { get; init; } = string.Empty;

    public DateOnly? Birthday { get; init; }
}
