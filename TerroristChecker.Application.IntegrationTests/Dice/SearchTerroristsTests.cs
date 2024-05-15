using FluentAssertions;

using TerroristChecker.Application.Cqrs.Queries;
using TerroristChecker.Application.Cqrs.Queries.SearchTerrorists;

namespace TerroristsChecker.Application.IntegrationTests.Dice;

using static Testing;

public class SearchTerroristsTests
{
    [Test]
    [TestCase("Abd al-Khaliq Badr al-Din al-Huthi", 2)]
    [TestCase("abdulkarimov mogamed usmanovich", 2)]
    [TestCase("Abd al-Khaliq Badr al-Din al-Huthi al", 0)]
    [TestCase("abdulkarimov mogamed usmanovich mogamed", 0)]
    public async Task SearchTerroristsQuery_ShouldReturnValidResults(string input, int expectedResultsCount)
    {
        // Arrange
        var query = new SearchTerroristsQuery(input, 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCount(expectedResultsCount);
    }

    [Test]
    [TestCase("abdulla MIKAILOVICH", "IUSUPOV ABDULLA MIKAILOVICH", "ABDULLAEV ABDULLA SAMAILOVICH should have lesser average coefficient than IUSUPOV ABDULLA MIKAILOVICH")]
    [TestCase("IDRISOVA AINA", "IDRISOVA AIDA UVAISOVNA", "AINA matches AIDA")]
    [TestCase("КИМ сик вон", "КИМ ЧОН СИК", "Short word of 3 letters with one mistake is a match")]
    public async Task SearchTerroristsQuery_ShouldReturnExpectedResults(string input, string fullName, string because)
    {
        // Arrange
        var query = new SearchTerroristsQuery(input);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().ContainSingle(x => x.NameFull == fullName, because);
    }
}
