namespace TerroristChecker.Domain.Dice.Models;

public sealed record PersonModel(
    int Id,
    PersonNameModel[] Names,
    string FullName,
    DateOnly? Birthday);
