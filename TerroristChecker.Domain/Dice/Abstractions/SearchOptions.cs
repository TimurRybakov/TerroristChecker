namespace TerroristChecker.Domain.Dice.Abstractions;

public sealed class SearchOptions
{
    public SearchOptions(DateOnly? birthday, int? yearOfBirth, double? minCoefficient, double? minAverageCoefficient)
    {
        Birthday = birthday;
        YearOfBirth = yearOfBirth;

        if (minCoefficient is not null)
            MinCoefficient = (double)minCoefficient;

        if (minAverageCoefficient is not null)
            MinAverageCoefficient = (double)minAverageCoefficient;
    }

    public SearchOptions()
    {

    }

    public DateOnly? Birthday { get; init; } = null;

    public int? YearOfBirth { get; init; } = null;

    public double MinCoefficient { get; init; } = 0.41;

    public double MinAverageCoefficient { get; init; } = 0.80;

    public static readonly SearchOptions Default = new ();
}
