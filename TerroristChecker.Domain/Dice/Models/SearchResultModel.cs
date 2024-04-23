namespace TerroristChecker.Domain.Dice.Models;

public record struct SearchResultModel(
    double AvgCoefficient,
    Dictionary<PersonNameModel, (int Count, sbyte? InputWordIndex, double Coefficient)> Names);
