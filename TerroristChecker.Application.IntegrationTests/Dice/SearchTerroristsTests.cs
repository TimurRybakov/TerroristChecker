using FluentAssertions;

using TerroristChecker.Application.Cqrs.Queries.SearchTerrorists;

namespace TerroristsChecker.Application.IntegrationTests.Dice;

public class SearchTerroristsTests(TestWebApplicationFactory factory) : TestWithSender(factory)
{
    [Theory]
    [InlineData("Abd al-Khaliq Badr al-Din al-Huthi", 4, "")]
    [InlineData("abdulkarimov magamed usmanovich", 4, "")]
    [InlineData("Abd al-Khaliq Badr al-Din al-Huthi al", 0, "One additional name 'al' makes whole name different")]
    [InlineData("IDRISOVA AINA", 0, "AINA is too different from AIDA")]
    [InlineData("КИМ сик вон", 0, "Short word of 3 letters with one mistake is not a match")]
    public async Task SearchTerroristsQuery_ShouldReturnValidResults(string input, int expectedResultsCount, string because)
    {
        // Arrange
        var query = new SearchTerroristsQuery(input, 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCount(expectedResultsCount, because);
    }

    [Theory]
    [InlineData("abdulla MIKAILOVICH", "IUSUPOV ABDULLA MIKAILOVICH", "ABDULLAEV ABDULLA SAMAILOVICH should have lesser average coefficient than IUSUPOV ABDULLA MIKAILOVICH")]
    public async Task SearchTerroristsQuery_ShouldReturnExpectedResults(string input, string fullName, string because)
    {
        // Arrange
        var query = new SearchTerroristsQuery(input);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().ContainSingle(x => x.NameFull == fullName, because);
    }
}
