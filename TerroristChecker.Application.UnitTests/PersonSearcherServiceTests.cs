using FluentAssertions;

using TerroristChecker.Application.Services;

namespace TerroristChecker.Application.UnitTests;

public class PersonSearcherServiceTests
{
    [Test]
    public void EvaluateBestMatch_ShouldReturnExpectedResults()
    {
        // Arrange
        var input = new Dictionary<int, double>[]
        {
            new() { [0] = 0.2,  [1] = 0.18, [3] = 0.22, [4] = 0.22 },
            new() { [0] = 0.22, [1] = 0.2,  [3] = 1,    [4] = 1 },
            new() { [0] = 0.18, [1] = 1,    [3] = 0.2,  [4] = 0.2 },
            new() { [0] = 0.22, [1] = 0.2,  [3] = 1,    [4] = 1 },
            new() { [2] = 1 }
        };

        var correctAnswer = new Dictionary<int, double>
        {
            [0] = 0.2, [1] = 1, [2] = 1, [3] = 1, [4] = 1
        };

        // Act
        var bestMatch = PersonSearcherService.HungarianBestMatch(input);

        //  Assert
        bestMatch.Should()
            .Equal(correctAnswer);
    }
}
