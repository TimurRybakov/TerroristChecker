using TerroristChecker.Domain.Dice.Abstractions;

namespace TerroristChecker.Domain.Dice.Models;

public readonly struct PersonNameModel(PersonModel person, byte wordIndex, string name, byte sameIndex, byte sameCount): INgramIndexKey
{
    public PersonModel Person { get; } = person;

    /// <summary>
    /// Index of the word in Person.FullName
    /// </summary>
    public byte WordIndex { get; } = wordIndex;

    public string Name { get; } = name;

    /// <summary>
    /// Index 1, 2, 3... of the word in Person.FullName in same words subset
    /// </summary>
    public byte SameIndex { get; } = sameIndex;

    /// <summary>
    /// Count of same words subset
    /// </summary>
    public byte SameCount { get; } = sameCount;

    public override string? ToString() => Name;

    public int GetNgramCount() => Name.Length - 2;
}
