using FluentAssertions;

using TerroristChecker.Application.Cqrs.Queries;

namespace TerroristsChecker.Application.IntegrationTests.Dice;

using static Testing;

public class SearchTerroristsTests
{
    [Test]
    [TestCase("Abd al-Khaliq Badr al-Din al-Huthi", 2)]
    [TestCase("abdulkarimov mogamed usmanovich", 2)]
    [TestCase("Abd al-Khaliq Badr al-Din al-Huthi notmatched", 0)]
    [TestCase("abdulkarimov mogamed usmanovich notmatched", 0)]
    public async Task SearchTerroristsQuery_ShouldReturnValidResults(string fullName, int expectedResultsCount)
    {
        // Arrange
        var query = new SearchTerroristsQuery(fullName, 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCount(expectedResultsCount);
    }
}
