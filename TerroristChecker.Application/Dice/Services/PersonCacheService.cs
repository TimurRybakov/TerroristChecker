using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Extensions.Logging;

using TerroristChecker.Application.Dice.Models;
using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Domain.Dice.Models;

namespace TerroristChecker.Application.Dice.Services;

public sealed class PersonCacheService(
    int capacity,
    ILogger<PersonCacheService> logger,
    IWordStorageService wordStorageService) : IPersonCacheService
{
    private readonly NgramIndex<PersonNameModel, PersonModel> _index = new(
        capacity * 2,
        PersonNameModelComparer.Instance,
        PersonModelComparer.Instance);

    public void Add(int id, string fullName, DateOnly? birthday)
    {
        var words = wordStorageService.ParseWords(fullName);

        if (words is [])
        {
            return;
        }

        if (words.Length > sbyte.MaxValue)
        {
            throw new ArithmeticException($"Number of words ({words.Length}) in fullName exceeded maximum allowed number of {sbyte.MaxValue}");
        }

        var person = new PersonModel(id, new PersonNameModel[words.Length], birthday);

        for (byte i = 0; i < words.Length; i++)
        {
            var word = wordStorageService.GetOrAdd(_index.GetPreparedInput(words[i]));

            var personName = new PersonNameModel(person, (sbyte)i, word);
            person.Names[i] = personName;
            _index.Add(word, personName, person);
        }
    }

    public void Clear()
    {
        wordStorageService.Clear();
        _index.Clear();
    }

    public Task<KeyValuePair<PersonModel, SearchResultModel>[]?> SearchAsync(
        string input,
        SearchOptions? searchOptions = null)
    {
        return Task.Run(() => Search(input, searchOptions));
    }

    public KeyValuePair<PersonModel, SearchResultModel>[]? Search(
        string input,
        SearchOptions? searchOptions = null)
    {
        logger.LogDebug("Input string: {Input}", input);
        var searchOptionsInternal = searchOptions ?? SearchOptions.Default;
        KeyValuePair<PersonModel, SearchResultModel>[]? orderedResults = null;
        try
        {
            var inputWords = wordStorageService.ParseWords(input);

            Dictionary<PersonModel, SearchResultModel>? accumulativeResults = null;

            for (sbyte inputWordIndex = 0; inputWordIndex < inputWords.Length; inputWordIndex++)
            {
                var word = inputWords[inputWordIndex];
                var preparedWord = _index.GetPreparedInput(word);

                if (!SearchWordWithLogging(
                        inputWord: preparedWord,
                        inputWordIndex: inputWordIndex,
                        accumulativeResults: ref accumulativeResults,
                        searchOptions: searchOptionsInternal) ||
                    accumulativeResults?.Count == 0)
                {
                    return null;
                }
            }

            if (accumulativeResults is not null)
            {
                foreach (var (person, searchResult) in accumulativeResults)
                {
                    ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(accumulativeResults, person);

                    value.AvgCoefficient = searchResult.Names.Average(x => x.Value.Coefficient);

                    if (value.AvgCoefficient < searchOptionsInternal.MinAverageCoefficient)
                    {
                        accumulativeResults.Remove(person);
                    }
                }
            }

            orderedResults = accumulativeResults?
                //.Where(x => x.Value.AvgCoefficient >= searchOptionsInternal.MinCoefficient)
                .OrderByDescending(x => x.Value.AvgCoefficient)
                .ToArray();

            if (logger.IsEnabled(LogLevel.Debug) && orderedResults?.Length > 0)
            {
                var personId = orderedResults[0].Key.Id;
                var personName = string.Join(' ', orderedResults[0].Key.Names);
                var personBirthday = orderedResults[0].Key.Birthday;
                var averageCoefficient = orderedResults[0].Value.AvgCoefficient;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SearchWordWithLogging(
        string inputWord,
        sbyte inputWordIndex,
        ref Dictionary<PersonModel, SearchResultModel>? accumulativeResults,
        SearchOptions searchOptions)
    {
        Dictionary<PersonNameModel, NgramSearchResultModel>? searchResults = null;

        try
        {
            var result = SearchWord(
                inputWord,
                inputWordIndex,
                ref accumulativeResults,
                searchOptions);

            searchResults = result.SearchResults;

            return result.Continue;
        }
        finally
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                var bestMatch = searchResults?.Count > 0
                    ? searchResults?.MaxBy(x => x.Value.Coefficient)
                    : null;

                if (bestMatch is null)
                {
                    logger.LogDebug("Input word: {InputWord} - no matches...", inputWord);
                }
                else
                {
                    var matchedWord = bestMatch.Value.Key.Name;
                    var inputNgramm = inputWord +
                                        (inputWord.Length < matchedWord.Length
                                           ? new string(' ', matchedWord.Length - inputWord.Length)
                                           : "") + " : " +
                                        string.Join(' ', _index.StringToNgramArray(inputWord).Select(x => x.Memory.ToString()));
                    var matchedNgramm = matchedWord +
                                          (matchedWord.Length < inputWord.Length
                                              ? new string(' ', inputWord.Length - matchedWord.Length)
                                              : "") + " : " +
                                          string.Join(' ', _index.StringToNgramArray(matchedWord).Select(x => x.Memory.ToString()));

                    logger.LogDebug("Input word: {InputNGgramm}, persons count: {PersonsCount}", inputNgramm, accumulativeResults?.Count);
                    logger.LogDebug("Best match: {MatchedNgramm}, coefficient: {Coefficient}, matches: {Matches} of {TotalNgrams}",
                         matchedNgramm, bestMatch.Value.Value.Coefficient, bestMatch.Value.Value.Matches,
                         bestMatch.Value.Key.GetNgramCount());
                }
            }

            if (logger.IsEnabled(LogLevel.Trace) && accumulativeResults is not null)
            {
                // Warning: following code dramatically decreases application`s performance.
                // Avoid using trace level in production environment!
                var sb = new StringBuilder(32);

                foreach (var item in accumulativeResults
                             .OrderByDescending(x => x.Value.Names.Average(v => v.Value.Coefficient)))
                {
                    sb.Clear();
                    sb.Append($"Person {{ Id = {item.Key.Id}, Names = \"{string.Join(", ", item.Key.Names)}\"}}");
                    foreach (var (name, coefficient) in item.Value.Names)
                    {
                        sb.Append($", Name = \"{name}\", Coefficient = {coefficient}");
                    }

                    logger.LogTrace("{PersonCacheData}", sb);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (bool Continue, Dictionary<PersonNameModel, NgramSearchResultModel> SearchResults) SearchWord(
        string inputWord,
        sbyte inputWordIndex,
        ref Dictionary<PersonModel, SearchResultModel>? accumulativeResults,
        SearchOptions searchOptions)
    {
        var birthday = searchOptions.Birthday;
        var yearOfBirth = searchOptions.YearOfBirth;

        // Get person names matching input word
        var searchResults = _index.GetMatches(inputWord, searchOptions.MinCoefficient,
            (_, person) => (birthday is null && (yearOfBirth is null || yearOfBirth == person.Birthday?.Year))
                           || Nullable.Equals(birthday, person.Birthday));

        if (searchResults.Count == 0)
            return (Continue: false, searchResults);

        // Convert person names dictionary to persons
        var convertedResults = GroupNamesToPersons(searchResults, inputWordIndex);

        // If there is no accumulative results yet then simply set them to converted dictionary and exit
        if (accumulativeResults is null)
        {
            accumulativeResults = convertedResults;
            //return (Continue: true, searchResults);
        }

        // Exclude persons from accumulative results that does not match persons dictionary
        foreach (var (person, accumulativeResult) in accumulativeResults)
        {
            var personMatched = false;

            // Search for person by every name
            foreach (var name in person.Names)
            {
                // Try get value from searchResults
                if (searchResults.TryGetValue(name, out var matchResults))
                {
                    personMatched = true;
                }

                // Update accumulative results with coefficient
                ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    accumulativeResult.Names, name, out var valExists);

                // If searchResults does not has value, continue
                if (matchResults.Matches == 0)
                {
                    continue;
                }

                if (valExists)
                {
                    // Update accumulativeResults
                    if (val.InputWordIndex is null)
                    {
                        val.Coefficient = matchResults.Coefficient;
                        val.InputWordIndex = inputWordIndex;
                    }

                    // Double match on the same name by two different input words
                    //val.Coefficient = (val.Coefficient + coefficient) / 2;
                    //val.InputWordIndex = inputWordIndex;
                }
            }

            if (!personMatched)
            {
                accumulativeResults.Remove(person);
            }
        }

        return (Continue: searchResults.Count != 0, searchResults);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Dictionary<PersonModel, SearchResultModel> GroupNamesToPersons(
        Dictionary<PersonNameModel, NgramSearchResultModel> results, sbyte? inputWordIndex)
    {
        var groupedResults = results
            .GroupBy(kvp => kvp.Key.Person,
                kvp => kvp)
            .ToDictionary(
                group => group.Key,
                group => new SearchResultModel(
                    AvgCoefficient: 0,
                    Names: group.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (
                            Count: group.Key.Names
                                .Count(names => string.Equals(names.Name, kvp.Key.Name)),
                            inputWordIndex: inputWordIndex,
                            kvp.Value.Coefficient))
                ),
                PersonModelComparer.Instance
            );

        return groupedResults;
    }
}
