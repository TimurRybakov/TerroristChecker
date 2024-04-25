namespace TerroristChecker.Domain.Dice.Models;

public record struct NameSearchResultModel(Dictionary<PersonNameModel, NameSearchResultValueModel> Names);

public record struct NameSearchResultValueModel(byte InputWordIndex, double Coefficient);
