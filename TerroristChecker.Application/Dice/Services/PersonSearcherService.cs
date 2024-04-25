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
        var words = wordStorageService.ParseWords(fullName);

        if (words is [])
        {
            return;
        }

        if (words.Length > byte.MaxValue)
        {
            throw new ArithmeticException(
                $"Number of words ({words.Length}) in fullName exceeded maximum allowed number of {byte.MaxValue}");
        }

        var person = new PersonModel(id, new PersonNameModel[words.Length], fullName, birthday);

        var prepares = words
            .Select((x, i) => new { PreparedWord = wordStorageService.GetOrAdd(_index.GetPreparedInput(x)), Index = i })
            .GroupBy(x => x.PreparedWord, StringComparer.InvariantCultureIgnoreCase)
            .SelectMany(group =>
                group.Select((word, index) => new
                {
                    Word = word.PreparedWord,
                    Index = (byte)word.Index,
                    SameCount = (byte)(group.Count()),
                    SameIndex = (byte)(index + 1)
                })
            );

        foreach (var prepared in prepares)
        {
            var personName = new PersonNameModel(person, prepared.Index, prepared.Word, prepared.SameIndex, prepared.SameCount);
            person.Names[prepared.Index] = personName;
            _index.Add(prepared.Word, personName, person);
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
            var inputWords = wordStorageService.ParseWords(input);
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
                        var preparedWord = _index.GetPreparedInput(word);
                        var result = SearchPersonsByWord(preparedWord, (byte)inputWordIndex, searchOptionsInternal);

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

            var result = IntersectMany(results);

            orderedResults = result
                .Select(x =>
                    (Person: x, AvgCoefficient: x.Value.Names.Average(name => name.Value.Coefficient)))
                .Where(x => x.AvgCoefficient >= searchOptionsInternal.MinAverageCoefficient)
                .OrderByDescending(x => x.AvgCoefficient)
                .ToArray();

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (bool Continue, Dictionary<PersonModel, NameSearchResultModel> Persons) SearchPersonsByWord(
        string inputWord,
        byte inputWordIndex,
        SearchOptions searchOptions)
    {
        var birthday = searchOptions.Birthday;
        var yearOfBirth = searchOptions.YearOfBirth;

        // Get person names matching input word
        var personNames = _index.GetMatches(
            inputWord, searchOptions.MinCoefficient,
            (_, person) => (birthday is null && (yearOfBirth is null || yearOfBirth == person.Birthday?.Year)) ||
                           Nullable.Equals(birthday, person.Birthday));

        if (personNames.Count == 0)
            return (Continue: false, Persons: new Dictionary<PersonModel, NameSearchResultModel>());

        // Convert person names dictionary to persons
        var persons = GroupNamesToPersons(personNames, inputWordIndex);

        return (Continue: persons.Count != 0, Persons: persons);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Dictionary<PersonModel, NameSearchResultModel> GroupNamesToPersons(
        Dictionary<PersonNameModel, NgramSearchResultModel> results, byte inputWordIndex)
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
                            inputWordIndex,
                            kvp.Value.Coefficient))
                ),
                PersonModelComparer.Instance
            );

        return groupedResults;
    }

    private static Dictionary<PersonModel, NamesSearchResultModel> IntersectMany(
        params Dictionary<PersonModel, NameSearchResultModel>[] arrayOfMatches)
    {
        if (arrayOfMatches.Length == 0)
        {
            return new Dictionary<PersonModel, NamesSearchResultModel>(PersonModelComparer.Instance);
        }

        Dictionary<PersonModel, NamesSearchResultModel> intersectedDictionary =
            new(arrayOfMatches.Min(x => x.Count), PersonModelComparer.Instance);

        foreach (var (person, searchResult) in arrayOfMatches[0])
        {
            // Check if person match intersects through all results
            if (!PersonExistsInAllMatches(person, arrayOfMatches))
            {
                continue;
            }

            var namesSearchResult = CreateNamesSearchResult(arrayOfMatches, person);

            // Add combined result to intersectedDictionary
            if (namesSearchResult.Names.Count > 0)
            {
                intersectedDictionary.Add(person, namesSearchResult);
            }
        }

        return intersectedDictionary;
    }

    private static NamesSearchResultModel CreateNamesSearchResult(
        Dictionary<PersonModel, NameSearchResultModel>[] arrayOfMatches,
        PersonModel person)
    {
        var namesDictionary = new Dictionary<PersonNameModel, NamesSearchResultValueModel>(PersonNameModelComparer.Instance);
        var namesSearchResult = new NamesSearchResultModel(Names: namesDictionary);

        // Add matches from other words
        foreach (var name in person.Names)
        {
            var inputNameMatches = new HashSet<int>(10);

            foreach (var match in arrayOfMatches)
            {
                var otherSearchResult = match[person];

                if (otherSearchResult.Names.TryGetValue(name, out var nameSearchResultValue))
                {
                    inputNameMatches.Add(nameSearchResultValue.InputWordIndex);

                    if (inputNameMatches.Count > name.SameCount)
                    {
                        namesDictionary.Clear();
                        return namesSearchResult;
                    }

                    ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(
                        namesDictionary, name, out var valExists);

                    if (!valExists)
                    {
                        val.Coefficient = nameSearchResultValue.Coefficient;
                    }
                }
            }

            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(
                namesDictionary, name, out var valueExists);

            if (valueExists)
            {
                if (inputNameMatches.Count == 0 || name.SameIndex > inputNameMatches.Count)
                {
                    value.Coefficient = 0;
                }
            }
        }

        return namesSearchResult;
    }

    private static bool PersonExistsInAllMatches(
        PersonModel person,
        IEnumerable<Dictionary<PersonModel, NameSearchResultModel>> arrayOfMatches)
    {
        var personExistsInAllMatches = true;
        foreach (var persons in arrayOfMatches)
        {
            if (!persons.ContainsKey(person))
            {
                personExistsInAllMatches = false;
                break;
            }
        }

        return personExistsInAllMatches;
    }
}
