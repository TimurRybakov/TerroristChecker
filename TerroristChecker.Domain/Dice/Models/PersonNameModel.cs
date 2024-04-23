using TerroristChecker.Domain.Dice.Abstractions;

namespace TerroristChecker.Domain.Dice.Models;

public readonly struct PersonNameModel(PersonModel person, sbyte wordIndex, string name): INgramIndexKey
{
    public PersonModel Person { get; } = person;

    public sbyte WordIndex { get; } = wordIndex;

    public string Name { get; } = name;

    public override string? ToString() => Name;

    public int GetNgramCount() => Name.Length - 2;
}
