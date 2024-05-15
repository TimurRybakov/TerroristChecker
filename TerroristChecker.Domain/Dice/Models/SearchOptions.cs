namespace TerroristChecker.Domain.Dice.Models;

public sealed class SearchOptions
{
    public SearchOptions(
        DateOnly? birthday,
        int? yearOfBirth,
        double? minCoefficient,
        double? minAverageCoefficient,
        bool? averageByInputCount)
    {
        Birthday = birthday;
        YearOfBirth = yearOfBirth;

        if (minCoefficient is not null)
            MinCoefficient = (double)minCoefficient;

        if (minAverageCoefficient is not null)
            MinAverageCoefficient = (double)minAverageCoefficient;

        if (averageByInputCount is not null)
            AverageByInputCount = (bool)averageByInputCount;
    }

    public SearchOptions()
    {

    }

    public DateOnly? Birthday { get; init; } = null;

    public int? YearOfBirth { get; init; } = null;

    public double MinCoefficient { get; init; } = 0.39;

    public double MinAverageCoefficient { get; init; } = 0.75;

    public static readonly SearchOptions Default = new ();


    /// <summary>
    /// If true counts max average coefficient only by input word count. If person name contains more words than in input
    /// it wont affect the average value.
    /// </summary>
    public bool AverageByInputCount { get; set; } = true;
}
