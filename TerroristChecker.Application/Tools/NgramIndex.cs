using System.Runtime.InteropServices;

using TerroristChecker.Application.Models;
using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Domain.Dice.ValueObjects;

namespace TerroristChecker.Application.Tools;

internal sealed class NgramIndex<TIndexKey, TIndexValue>(int capacity, int? n = null)
    where TIndexKey : INgramIndexKey
    where TIndexValue : notnull
{
    private Dictionary<Ngram, Dictionary<TIndexKey, TIndexValue>> _ngramDict { get; } =
        new(capacity, NgramComparer.Instance);

    private int _n = n ?? 3;

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

            if (_n != value)
            {
                _n = value;

                SetBeginning();
                SetEnding();
            }
        }
    }

    private string _beginning = string.Empty;

    private string _ending = string.Empty;

    private void SetBeginning()
    {
        _beginning = new string('[', _n - 1);
    }

    private void SetEnding()
    {
        _ending = new string(']', _n - 1);
    }

    public IEqualityComparer<TIndexKey>? IndexKeyEqualityComparer { get; }

    public IEqualityComparer<TIndexValue>? IndexValueEqualityComparer { get; }

    public NgramIndex(
        int capacity,
        int? n,
        IEqualityComparer<TIndexKey>? indexKeyEqualityComparer,
        IEqualityComparer<TIndexValue>? indexValueEqualityComparer = null) : this(capacity, n)
    {
        IndexKeyEqualityComparer = indexKeyEqualityComparer;
        IndexValueEqualityComparer = indexValueEqualityComparer;

        SetBeginning();
        SetEnding();
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
                _ngramDict, nGram, out var nGramDictValueExists);

            if (!nGramDictValueExists)
            {
                nGramDictValue = new Dictionary<TIndexKey, TIndexValue>(IndexKeyEqualityComparer);
            }

            nGramDictValue![key] = value;
        }
    }

    public void Clear()
    {
        _ngramDict.Clear();
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
        var n = N;

        foreach (var nGram in nGrams)
        {
            if (!_ngramDict.TryGetValue(nGram, out var indexValues))
            {
                continue;
            }

            foreach (var index in indexValues)
            {
                if (externalFilter is not null && !externalFilter(index.Key, index.Value))
                {
                    continue;
                }

                ref var resultVal = ref CollectionsMarshal.GetValueRefOrAddDefault(result, index.Key, out var resultValExists);

                if (resultValExists)
                {
                    resultVal.Matches++;
                    resultVal.Coefficient = 2 * resultVal.Matches / (double)(nGrams.Length + index.Key.GetLength() - (n - 1));
                }
                else
                {
                    resultVal.Matches = 1;
                    resultVal.Coefficient = 2 / (double)(nGrams.Length + index.Key.GetLength() - (n - 1));
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

        return string.Intern(string.Concat(_beginning, input.ToUpperInvariant(), _ending));
    }
}
