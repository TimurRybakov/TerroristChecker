namespace TerroristChecker.Domain.Dice.Models;

public record struct NamesSearchResultModel(Dictionary<PersonNameModel, NamesSearchResultValueModel> Names);

public record struct NamesSearchResultValueModel(byte InputWordIndex, double Coefficient);
