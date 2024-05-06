using System.Runtime.InteropServices;

using TerroristChecker.Application.Dice.Models;
using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Domain.Dice.ValueObjects;

namespace TerroristChecker.Application.Dice;

internal sealed class NgramIndex<TIndexKey, TIndexValue>(int capacity)
    where TIndexKey : INgramIndexKey
    where TIndexValue : notnull
{
    private Dictionary<Ngram, Dictionary<TIndexKey, TIndexValue>> NgramDict { get; } =
        new(capacity, NgramComparer.Instance);

    private int _n = 3;

    // Length of n-gram to use (recommended number is '3', trigram)
    public int N
    {
        get => _n;
        set
        {
            if (value is < 2 or > 10)
            {
                throw new ArgumentException("N should be >= 2 and <= 10.");
            }

            _n = value;
        }
    }

    public IEqualityComparer<TIndexKey>? IndexKeyEqualityComparer { get; }

    public IEqualityComparer<TIndexValue>? IndexValueEqualityComparer { get; }

    public NgramIndex(
        int capacity,
        IEqualityComparer<TIndexKey>? indexKeyEqualityComparer,
        IEqualityComparer<TIndexValue>? indexValueEqualityComparer = null) : this(capacity)
    {
        IndexKeyEqualityComparer = indexKeyEqualityComparer;
        IndexValueEqualityComparer = indexValueEqualityComparer;
    }

    public Ngram[] StringToNgramArray(string input)
    {
        var n = N;

        var length = input.Length;
        var nGrams = new Ngram[length - (n - 1)];

        for (var i = 0; i < length - (n - 1); i++)
        {
            var nGram = new Ngram(input.AsMemory(i, n));
            nGrams[i] = nGram;
        }

        return nGrams;
    }

    public void Add(string input, TIndexKey key, TIndexValue value)
    {
        var nGrams = StringToNgramArray(input);

        foreach (var nGram in nGrams)
        {
            ref var nGramDictValue = ref CollectionsMarshal.GetValueRefOrAddDefault(
                NgramDict, nGram, out var nGramDictValueExists);

            if (!nGramDictValueExists)
            {
                nGramDictValue = new Dictionary<TIndexKey, TIndexValue>(IndexKeyEqualityComparer);
            }

            nGramDictValue![key] = value;
        }
    }

    public void Clear()
    {
        NgramDict.Clear();
    }

    public Dictionary<TIndexKey, NgramSearchResultModel> GetMatches(
        string input,
        double minCoefficient,
        Func<TIndexKey, TIndexValue, bool>? externalFilter = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input is null or white space.");
        }

        Dictionary<TIndexKey, NgramSearchResultModel> result = new(64, IndexKeyEqualityComparer);

        var nGrams = StringToNgramArray(input);

        foreach (var nGram in nGrams)
        {
            if (!NgramDict.TryGetValue(nGram, out var indexValues))
            {
                continue;
            }

            foreach (var (indexKey, indexValue) in indexValues)
            {
                if (externalFilter is not null && !externalFilter(indexKey, indexValue))
                {
                    continue;
                }

                ref var resultVal = ref CollectionsMarshal.GetValueRefOrAddDefault(result, indexKey, out var resultValExists);

                if (resultValExists)
                {
                    resultVal.Matches++;
                    resultVal.Coefficient = 2 * resultVal.Matches / (double)(nGrams.Length + indexKey.GetNgramCount());
                }
                else
                {
                    resultVal.Matches = 1;
                    resultVal.Coefficient = 2 / (double)(nGrams.Length + indexKey.GetNgramCount());
                }
            }
        }

        if (minCoefficient < 1)
        {
            foreach (var kvp in result)
            {
                if (kvp.Value.Coefficient < minCoefficient)
                {
                    result.Remove(kvp.Key);
                }
            }
        }

        return result;
    }

    public string PrepareWord(string input)
    {
        var n = N;

        return string.Intern(new string('[', n - 1) + input.ToUpperInvariant() + new string(']', n - 1));
    }
}
