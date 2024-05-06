using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

using TerroristChecker.Application.Dice.Models;
using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Application.Dice.Services;

public sealed class PersonSearcherService(
    int capacity,
    ILogger<PersonSearcherService> logger,
    IWordStorageService wordStorageService) : IPersonSearcherService
{
    private readonly NgramIndex<PersonNameModel, PersonModel> _index = new(
        capacity * 2,
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
            var personName = new PersonNameModel(person, word.Index, word.Value, word.SameIndex, word.SameCount);
            person.Names[word.Index] = personName;
            _index.Add(word.Value, personName, person);
        }
    }

    public void Clear()
    {
        wordStorageService.Clear();
        _index.Clear();
    }

    public async Task<(KeyValuePair<PersonModel,NamesSearchResultModel> Person, double AvgCoefficient)[]?> SearchAsync(
        string input,
        SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Input string: {Input}", input);
        (KeyValuePair<PersonModel,NamesSearchResultModel> Person, double AvgCoefficient)[]? orderedResults = null;
        try
        {
            var inputWords = wordStorageService.ParseWords(input, w => _index.PrepareWord(w));
            if (inputWords.Length == 0)
            {
                return null;
            }

            var searchOptionsInternal = searchOptions ?? SearchOptions.Default;
            Dictionary<PersonModel, NameSearchResultModel>[] results =
                new Dictionary<PersonModel, NameSearchResultModel>[inputWords.Length];
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cts.Token
            };

            try
            {
                await Parallel.ForAsync(
                    0, inputWords.Length, options, async (inputWordIndex, ct) =>
                    {
                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }

                        var word = inputWords[inputWordIndex];
                        var result = SearchPersonsByWord(word, inputWords.Length, searchOptionsInternal);

                        results[inputWordIndex] = result.Persons;

                        if (!result.Continue || result.Persons.Count == 0)
                        {
                            await cts.CancelAsync();
                        }
                    });
            }
            catch (TaskCanceledException)
            {
                return null;
            }

            if (cts.Token.IsCancellationRequested)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var result = IntersectMany(inputWords, results);

            orderedResults = FilterAvgCoefficientAndOrderResults(inputWords, result, searchOptionsInternal);

            if (logger.IsEnabled(LogLevel.Debug) && orderedResults?.Length > 0)
            {
                var personId = orderedResults[0].Person.Key.Id;
                var personName = orderedResults[0].Person.Key.FullName;
                var personBirthday = orderedResults[0].Person.Key.Birthday;
                var averageCoefficient = orderedResults[0].AvgCoefficient;
                logger.LogDebug(
                    "Best match person Id = {PersonId}, Name = '{PersonName}', " +
                    "Birthday = {PersonBirthday}, Average coefficient: {AverageCoefficient}",
                    personId, personName, personBirthday, averageCoefficient);
            }

            return orderedResults;
        }
        finally
        {
            if (orderedResults?.Length == 0)
                logger.LogDebug("No matches for {Input}", input);
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
                            //Count: group.Key.Names.Count(names => string.Equals(names.Name, kvp.Key.Name)),
                            inputWord.Index,
                            kvp.Value.Coefficient))
                ),
                PersonModelComparer.Instance
            );

        return groupedResults;
    }

    private static Dictionary<PersonModel, NamesSearchResultModel> IntersectMany(
        WordModel[] inputWords,
        params Dictionary<PersonModel, NameSearchResultModel>[] arrayOfMatches)
    {
        if (arrayOfMatches.Length == 0)
        {
            return new Dictionary<PersonModel, NamesSearchResultModel>(PersonModelComparer.Instance);
        }

        Dictionary<PersonModel, NamesSearchResultModel> intersectedDictionary =
            new(arrayOfMatches.Min(x => x.Count), PersonModelComparer.Instance);

        // Iterate through each person to search for intersections
        foreach (var (person, _) in arrayOfMatches[0])
        {
            // Check if person match intersects through all results
            if (!PersonExistsInAllMatches(person, arrayOfMatches))
            {
                continue;
            }

            // Create results algorithm
            var namesSearchResult = CreateNamesSearchResult(inputWords, person, arrayOfMatches);

            // Add combined result to intersectedDictionary
            if (namesSearchResult.Names.Count > 0)
            {
                intersectedDictionary.Add(person, namesSearchResult);
            }
        }

        return intersectedDictionary;
    }

    private static NamesSearchResultModel CreateNamesSearchResult(
        WordModel[] inputWords,
        PersonModel person,
        Dictionary<PersonModel, NameSearchResultModel>[] arrayOfMatches)
    {
        var namesDictionary = new Dictionary<PersonNameModel, NamesSearchResultValueModel>(PersonNameModelComparer.Instance);
        var namesSearchResult = new NamesSearchResultModel(Names: namesDictionary);
        var inputNameMatches = new Dictionary<int, double>[person.Names.Length];
        var severalInputsForOnePersonName = false;

        // Iterate through ALL persons names (not only matched by index)
        foreach (var name in person.Names)
        {
            inputNameMatches[name.WordIndex] = new Dictionary<int, double>(10);
            var inputNameMatch = inputNameMatches[name.WordIndex];

            foreach (var match in arrayOfMatches)
            {
                var searchResult = match[person];

                // Search person name
                if (searchResult.Names.TryGetValue(name, out var nameSearchResultValue))
                {
                    // Number of same words in input exceeds same name count in person names - not a match!
                    if (inputWords[nameSearchResultValue.InputWordIndex].SameCount > name.SameCount)
                    {
                        namesDictionary.Clear();
                        return namesSearchResult;
                    }

                    inputNameMatch.Add(nameSearchResultValue.InputWordIndex, nameSearchResultValue.Coefficient);

                    if (!severalInputsForOnePersonName)
                    {
                        // This condition changes the algorithm (several inputs matched one person name)
                        if (inputNameMatch.Count > name.SameCount)
                        {
                            severalInputsForOnePersonName = true;
                            continue;
                        }

                        ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(
                            namesDictionary, name, out var valExists);

                        if (!valExists)
                        {
                            val.Coefficient = nameSearchResultValue.Coefficient;
                        }
                    }
                }
            }

            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(
                namesDictionary, name, out var valueExists);

            if (valueExists)
            {
                if (inputNameMatch.Count == 0 || name.SameIndex > inputNameMatch.Count)
                {
                    value.Coefficient = 0;
                }
            }
        }

        if (severalInputsForOnePersonName)
        {
            var bestMatch = EvaluateBestMatch(inputNameMatches);

            foreach (var name in person.Names)
            {
                ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    namesDictionary, name, out var valExists);

                if (bestMatch.TryGetValue(name.WordIndex, out var bestMatchValue))
                {
                    val.Coefficient = bestMatchValue;
                }
            }
        }

        return namesSearchResult;
    }

    /// <summary>
    /// Evaluation based om dynamic programming principle to find match with maximum average coefficient
    /// </summary>
    /// <param name="coefficients"></param>
    /// <returns></returns>
    private static Dictionary<int, double> EvaluateBestMatch(Dictionary<int, double>[] coefficients)
    {
        int n = coefficients.Length;
        int m = coefficients.Max(x => x.Count);

        double[,] dp = new double[n, m];
        int[,] path = new int[n, m];

        for (int i = 0; i < m; i++)
        {
            dp[0, i] = coefficients[0].GetValueOrDefault(i, 0);
        }

        for (int i = 1; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                dp[i, j] = 0;
                path[i, j] = 0;

                if (!coefficients[i].ContainsKey(j))
                    continue;

                for (int k = 0; k < m; k++)
                {
                    double maxCoefficientLocal = dp[i - 1, k] + coefficients[i][j];

                    if (maxCoefficientLocal > dp[i, j])
                    {
                        dp[i, j] = maxCoefficientLocal;
                        path[i, j] = k;
                    }
                }
            }
        }

        double maxCoefficient = dp[n - 1, 0];
        int lastIndex = 0;

        for (int i = 1; i < m; i++)
        {
            if (dp[n - 1, i] > maxCoefficient)
            {
                maxCoefficient = dp[n - 1, i];
                lastIndex = i;
            }
        }

        var maxCoefficients = new Dictionary<int, double>();
        for (int i = n - 1, j = lastIndex; i >= 0; i--)
        {
            if (coefficients[i].TryGetValue(j, out var value))
            {
                maxCoefficients.Add(i, value);
            }

            j = path[i, j];
        }

        return maxCoefficients;
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
