using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using TerroristChecker.Application.Dice.Models;
using TerroristChecker.Application.Tools;
using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Domain.Dice.Models;

[assembly: InternalsVisibleTo("TerroristChecker.Application.UnitTests")]

namespace TerroristChecker.Application.Dice.Services;

public sealed class PersonSearcherService(
    int capacity,
    ILogger<PersonSearcherService> logger,
    IWordStorageService wordStorageService,
    IConfiguration configuration) : IPersonSearcherService
{
    private readonly NgramIndex<PersonNameModel, PersonModel> _index = new(
        capacity * 2,
        configuration.GetValue<int?>("SearchAlgorithm:Ngrams"),
        PersonNameModelComparer.Instance,
        PersonModelComparer.Instance);

    public void Add(int id, string fullName, DateOnly? birthday)
    {
        var words = wordStorageService.ParseWords(
            fullName, w => wordStorageService.GetOrAdd(_index.PrepareWord(w)));

        if (words is [])
        {
            return;
        }

        var person = new PersonModel(id, new PersonNameModel[words.Length], fullName, birthday);

        foreach (var word in words)
        {
            var personName = new PersonNameModel(person, word.Index, word.Value);
            person.Names[word.Index] = personName;
            _index.Add(word.Value, personName, person);
        }
    }

    public void Clear()
    {
        wordStorageService.Clear();
        _index.Clear();
    }

    public (KeyValuePair<PersonModel,NamesSearchResultModel> Person, double AvgCoefficient)[]? Search(
        string input,
        SearchOptions? searchOptions = null)
    {
        (KeyValuePair<PersonModel,NamesSearchResultModel> Person, double AvgCoefficient)[]? orderedResults = null;
        try
        {
            var inputWords = wordStorageService.ParseWords(input, w => _index.PrepareWord(w));
            if (inputWords.Length == 0)
            {
                return null;
            }

            var searchOptionsInternal = searchOptions ?? SearchOptions.Default;
            Dictionary<PersonModel, NameSearchResultModel>[] inputWordsMatches =
                new Dictionary<PersonModel, NameSearchResultModel>[inputWords.Length];
            var stopped = false;
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.For(0, inputWords.Length, options, (inputWordIndex, state) =>
            {
                if (state.IsStopped)
                {
                    return;
                }

                var word = inputWords[inputWordIndex];
                var inputWordMatch = SearchPersonsByWord(word, inputWords.Length, searchOptionsInternal);

                inputWordsMatches[inputWordIndex] = inputWordMatch.Persons;

                if (!inputWordMatch.Continue || inputWordMatch.Persons.Count == 0)
                {
                    stopped = true;
                    state.Stop();
                }
            });

            if (stopped)
            {
                return null;
            }

            var results = IntersectMany(inputWords, searchOptionsInternal, inputWordsMatches);

            orderedResults = FilterAvgCoefficientAndOrderResults(inputWords, results, searchOptionsInternal);

            if (logger.IsEnabled(LogLevel.Information) && orderedResults.Length > 0)
            {
                var result = orderedResults[0];
                var personId = result.Person.Key.Id;
                var personName = result.Person.Key.FullName;
                var personBirthday = result.Person.Key.Birthday;
                var averageCoefficient = result.AvgCoefficient;
                logger.LogInformation(
                    "Input string: {Input}: Best match person Id = {PersonId}, Name = '{PersonName}', " +
                    "Birthday = {PersonBirthday}, Average coefficient: {AverageCoefficient}",
                    input, personId, personName, personBirthday, averageCoefficient);
            }

            return orderedResults;
        }
        finally
        {
            if (orderedResults?.Length == 0)
                logger.LogInformation("Input string: {Input}: No matches", input);
        }
    }

    private static (KeyValuePair<PersonModel, NamesSearchResultModel> Person, double AvgCoefficient)[]
        FilterAvgCoefficientAndOrderResults(
            WordModel[] inputWords,
            Dictionary<PersonModel, NamesSearchResultModel> result,
            SearchOptions searchOptions)
    {
        return result.Select(x =>
            {
                var avgCoefficient = searchOptions.AverageByInputCount
                    ? x.Value.Names
                        .OrderByDescending(y => y.Value.Coefficient)
                        .Take(inputWords.Length)
                        .Average(name => name.Value.Coefficient)
                    : x.Value.Names
                        .Average(name => name.Value.Coefficient);

                return (Person: x, AvgCoefficient: avgCoefficient);
            })
            .Where(x => x.AvgCoefficient >= searchOptions.MinAverageCoefficient)
            .OrderByDescending(x => x.AvgCoefficient)
            .ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (bool Continue, Dictionary<PersonModel, NameSearchResultModel> Persons) SearchPersonsByWord(
        WordModel inputWord,
        int inputWordsCount,
        SearchOptions searchOptions)
    {
        var birthday = searchOptions.Birthday;
        var yearOfBirth = searchOptions.YearOfBirth;

        // Get person names matching input word
        var personNames = _index.GetMatches(
            inputWord.Value,
            searchOptions.MinCoefficient,
            (_, person) => (
                (birthday is null && (yearOfBirth is null || yearOfBirth == person.Birthday?.Year))
                || Nullable.Equals(birthday, person.Birthday)
                ) && person.Names.Length >= inputWordsCount);

        if (personNames.Count == 0)
            return (Continue: false, Persons: new Dictionary<PersonModel, NameSearchResultModel>());

        // Convert person names dictionary to persons
        var persons = GroupNamesToPersons(personNames, inputWord);

        return (Continue: persons.Count != 0, Persons: persons);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Dictionary<PersonModel, NameSearchResultModel> GroupNamesToPersons(
        Dictionary<PersonNameModel, NgramSearchResultModel> results, WordModel inputWord)
    {
        var groupedResults = results
            .GroupBy(
                kvp => kvp.Key.Person,
                kvp => kvp)
            .ToDictionary(
                group => group.Key,
                group => new NameSearchResultModel(
                    Names: group.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new NameSearchResultValueModel(
                            inputWord.Index,
                            kvp.Value.Coefficient))
                ),
                PersonModelComparer.Instance
            );

        return groupedResults;
    }

    private static Dictionary<PersonModel, NamesSearchResultModel> IntersectMany(
        WordModel[] inputWords,
        SearchOptions searchOptions,
        params Dictionary<PersonModel, NameSearchResultModel>[] inputWordsMatches)
    {
        if (inputWordsMatches.Length == 0)
        {
            return new Dictionary<PersonModel, NamesSearchResultModel>(PersonModelComparer.Instance);
        }

        // Optimization: better start from the smallest result
        inputWordsMatches = MoveSmallestResultFirst(inputWordsMatches);

        var inputWordsMatch = inputWordsMatches[0];

        Dictionary<PersonModel, NamesSearchResultModel> intersectedDictionary =
            new(inputWordsMatch.Count, PersonModelComparer.Instance);

        int chunkSize = inputWordsMatch.Count / Environment.ProcessorCount;
        if (chunkSize == 0)
        {
            chunkSize = 1;
        }

        Parallel.ForEach(inputWordsMatch.Chunk(chunkSize), chunk =>
        {
            // Iterate through each person to search for intersections
            foreach (var item in chunk)
            {
                // If person has fewer names than in input, skip it
                if (item.Key.Names.Length < inputWords.Length)
                {
                    continue;
                }

                // Check if person match intersects through all results
                if (!PersonExistsInAllMatches(item.Key, inputWordsMatches))
                {
                    continue;
                }

                // Create results algorithm
                var namesSearchResult = CreateNamesSearchResult(
                    inputWords, item.Key, inputWordsMatches, searchOptions);

                // Add combined result to intersectedDictionary
                if (namesSearchResult.Names.Count > 0)
                {
                    intersectedDictionary.Add(item.Key, namesSearchResult);
                }
            }
        });

        return intersectedDictionary;
    }

    private static Dictionary<PersonModel, NameSearchResultModel>[] MoveSmallestResultFirst(Dictionary<PersonModel, NameSearchResultModel>[] inputWordsMatches)
    {
        int pos = 0;
        for (int i = 1; i < inputWordsMatches.Length; i++)
        {
            if (inputWordsMatches[i].Count < inputWordsMatches[pos].Count)
            {
                pos = i;
            }
        }

        if (pos > 0)
        {
            (inputWordsMatches[0], inputWordsMatches[pos]) = (inputWordsMatches[pos], inputWordsMatches[0]);
        }

        return inputWordsMatches;
    }

    private enum CoefficientProcessingAlgorithm : byte
    {
        None,
        Simple, // One input word matched several person names. Works only if searchOptions.AverageByInputCount == false
        Hungarian // One person name matched several input words
    }

    private static NamesSearchResultModel CreateNamesSearchResult(
        WordModel[] inputWords,
        PersonModel person,
        Dictionary<PersonModel, NameSearchResultModel>[] arrayOfMatches,
        SearchOptions searchOptions)
    {
        var namesDictionary = new Dictionary<PersonNameModel, NamesSearchResultValueModel>(PersonNameModelComparer.Instance);
        var inputNameMatches = new Dictionary<int, double>[person.Names.Length];
        var algorithm = CoefficientProcessingAlgorithm.None;
        var inputWordsIndices = searchOptions.AverageByInputCount ? null : new HashSet<int>(inputWords.Length);

        // Iterate through ALL persons names (not only matched by index)
        foreach (var name in person.Names)
        {
            inputNameMatches[name.WordIndex] = new Dictionary<int, double>(10);
            var inputNameMatch = inputNameMatches[name.WordIndex];

            foreach (var match in arrayOfMatches)
            {
                var searchResult = match[person];

                // Search person name
                if (!searchResult.Names.TryGetValue(name, out var nameSearchResultValue))
                {
                    continue;
                }

                inputNameMatch.Add(nameSearchResultValue.InputWordIndex, nameSearchResultValue.Coefficient);

                if (algorithm != CoefficientProcessingAlgorithm.Hungarian)
                {
                    if (inputNameMatch.Count > 1)
                    {
                        algorithm = CoefficientProcessingAlgorithm.Hungarian;
                        continue;
                    }

                    ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(
                        namesDictionary, name, out var valExists);

                    if (!valExists)
                    {
                        val.Coefficient = nameSearchResultValue.Coefficient;
                        val.InputWordIndex = nameSearchResultValue.InputWordIndex;
                    }

                    if (inputWordsIndices is not null && !inputWordsIndices.Add(nameSearchResultValue.InputWordIndex))
                    {
                        algorithm = CoefficientProcessingAlgorithm.Simple;

                        // Correct coefficient taking maximum
                        foreach (var (key, value) in namesDictionary)
                        {
                            if (value.InputWordIndex == nameSearchResultValue.InputWordIndex)
                            {
                                if (value.Coefficient > 0 && value.Coefficient < nameSearchResultValue.Coefficient)
                                {
                                    ref var correctedValue = ref namesDictionary.GetValueRefOrAddDefault(key);
                                    correctedValue.Coefficient = 0;
                                }
                                else if (val.Coefficient > 0 && value.Coefficient > nameSearchResultValue.Coefficient)
                                {
                                    val.Coefficient = 0;
                                }
                            }
                        }
                    }
                }
            }

            if (!searchOptions.AverageByInputCount)
            {
                // Ensure default zero coefficient is added if person name not matched any input
                CollectionsMarshal.GetValueRefOrAddDefault(namesDictionary, name, out _);
            }
        }

        if (algorithm == CoefficientProcessingAlgorithm.Hungarian)
        {
            var bestMatch = HungarianBestMatch(inputNameMatches);

            foreach (var name in person.Names)
            {
                ref var val = ref namesDictionary.GetValueRefOrAddDefault(name);

                if (bestMatch.TryGetValue(name.WordIndex, out var bestMatchValue))
                {
                    val.Coefficient = bestMatchValue;
                }
            }
        }

        return new NamesSearchResultModel(Names: namesDictionary);
    }

    /// <summary>
    /// Evaluation based on hungarian algorithm to find match with maximum average coefficient
    /// </summary>
    /// <param name="coefficients"></param>
    /// <returns></returns>
    internal static Dictionary<int, double> HungarianBestMatch(Dictionary<int, double>[] coefficients)
    {
        var matrix = ArrayOfDictionariesToMatrix(coefficients);

        var result = matrix.FindAssignments(HungarianAlgorithm.ExtremumType.Max);

        var maxCoefficients = new Dictionary<int, double>(result.Length);
        for (int i = 0; i < result.Length; i++)
        {
            if (coefficients[i].TryGetValue(result[i], out var value))
            {
                maxCoefficients.Add(i, value);
            }
            else
            {
                maxCoefficients.Add(i, 0);
            }
        }

        return maxCoefficients;
    }

    private static int[,] ArrayOfDictionariesToMatrix(Dictionary<int, double>[] coefficients)
    {
        int rows = coefficients.Length;
        int cols = coefficients.Max(d => d.Count > 0 ? d.Keys.Max() : 0) + 1;

        // Matrix should be squared or algorithm may enter never ending loop if rows > cols!
        if (rows != cols)
        {
            var max = Math.Max(rows, cols);
            rows = max;
            cols = max;
        }

        var matrix = new int[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            foreach (var kvp in coefficients[i])
            {
                matrix[i, kvp.Key] = (int)(kvp.Value * 10_000);
            }
        }

        return matrix;
    }

    private static bool PersonExistsInAllMatches(
        PersonModel person,
        Dictionary<PersonModel, NameSearchResultModel>[] arrayOfMatches)
    {
        var personExistsInAllMatches = true;
        for (var index = 1; index < arrayOfMatches.Length; index++)
        {
            var persons = arrayOfMatches[index];
            if (!persons.ContainsKey(person))
            {
                personExistsInAllMatches = false;
                break;
            }
        }

        return personExistsInAllMatches;
    }
}
