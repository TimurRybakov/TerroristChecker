using FluentAssertions;

using TerroristChecker.Application.Cqrs.Queries;
using TerroristChecker.Application.Cqrs.Queries.GetTerrorists;

namespace TerroristsChecker.Application.IntegrationTests.Dice;

using static Testing;

public class GetTerroristsTests
{
    [Test]
    public async Task GetTerroristsQuery_ShouldReturnNonEmptyList()
    {
        // Arrange
        var query = new GetTerroristsQuery();

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCountGreaterThan(0);
    }
}
