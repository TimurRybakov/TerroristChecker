namespace TerroristChecker.Domain.Dice.Models;

public sealed class PersonModelComparer : IEqualityComparer<PersonModel>
{
    public static readonly PersonModelComparer Instance = new();

    public bool Equals(PersonModel? a, PersonModel? b) => a?.Id == b?.Id;

    public int GetHashCode(PersonModel o) => o.Id.GetHashCode();
}
