using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using TerroristChecker.Application.Services;
using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Application.UnitTests;

public class PersonSearcherServiceTests
{
    private readonly IWordStorageService _wordStorageService = new WordStorageService(10);

    private readonly IPersonSearcherService _personSearcherService;

    private readonly IConfiguration _configuration;

    public PersonSearcherServiceTests()
    {
        var loggerMock = new Mock<ILogger<PersonSearcherService>>();

        var wordStorageService = new WordStorageService(10);

        Dictionary<string, string> configuratonDictionary = new Dictionary<string, string>
        {
            {"SearchAlgorithm:Ngrams", "3"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(initialData: configuratonDictionary!)
            .Build();

        _personSearcherService = new PersonSearcherService(10, loggerMock.Object, wordStorageService, _configuration);

        _personSearcherService.Add(1, "EL MUHAMMED HALED", DateOnly.Parse("1982-10-03"));
        _personSearcherService.Add(2, "MUMAR AL IBN AL MOHAMMED", null);
        _personSearcherService.Add(3, "MOGAMED IBN KHALED", null);
        _personSearcherService.Add(4, "MUAMAR IBN HASAN", null);
        _personSearcherService.Add(5, "GASAN MUMAROV", null);
        _personSearcherService.Add(6, "MUHAMMED OMAR", null);
        _personSearcherService.Add(7, "HALED HASAN", null);
    }

    [Fact]
    public void HungarianBestMatch_ShouldReturnExpectedResults()
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

        // Assert
        bestMatch.Should()
            .Equal(correctAnswer);
    }

    [Fact]
    public void PersonSearcherService_ComplexSearchShouldUseHungarianAlggorithm()
    {
        // Arrange
        var searchOptions = new SearchOptions()
        {
            MinAverageCoefficient = 0.5,
            MinCoefficient = 0.5,
            AverageByInputCount = false
        };

        // Act
        var result = _personSearcherService.Search("mumr al mohammed", searchOptions);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(x => x.Person.Key.FullName == "MUMAR AL IBN AL MOHAMMED");
    }
}
