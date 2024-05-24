using TerroristChecker.Domain.Dice.Abstractions;

namespace TerroristChecker.Domain.Dice.Models;

public sealed record PersonNameModel(PersonModel Person, byte WordIndex, string Name): INgramIndexKey
{
    public override string? ToString() => Name;

    public int GetLength() => Name.Length;
}
