namespace TerroristChecker.Domain.Dice.Models;

public sealed class PersonNameModelComparer : IEqualityComparer<PersonNameModel>
{
    public static readonly PersonNameModelComparer Instance = new();

    public bool Equals(PersonNameModel? a, PersonNameModel? b) =>
        a?.Person.Id == b?.Person.Id && a?.WordIndex == b?.WordIndex;

    public int GetHashCode(PersonNameModel o) =>
        EqualityComparer<PersonNameModel>.Default.GetHashCode(o);
}
