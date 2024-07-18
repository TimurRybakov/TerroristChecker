using FluentAssertions;

using TerroristChecker.Application.Cqrs.Queries.GetTerrorists;

namespace TerroristsChecker.Application.IntegrationTests.Dice;

public class GetTerroristsTests(TestWebApplicationFactory factory) : TestWithSender(factory)
{
    [Fact]
    public async Task GetTerroristsQuery_ShouldReturnNonEmptyList()
    {
        // Arrange
        var query = new GetTerroristsQuery();

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCountGreaterThan(0);
    }
}
