namespace TerroristChecker.Domain.Dice.Models;

public record struct PersonModel(int Id, PersonNameModel[] Names, DateOnly? Birthday);
