namespace TerroristChecker.Domain.Dice.Models;

public record struct PersonModel(
    int Id,
    PersonNameModel[] Names,
    string FullName,
    DateOnly? Birthday);
