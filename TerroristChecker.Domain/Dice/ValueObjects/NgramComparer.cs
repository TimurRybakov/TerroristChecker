namespace TerroristChecker.Domain.Dice.ValueObjects;

public sealed class NgramComparer : IEqualityComparer<Ngram>
{
    public static readonly NgramComparer Instance = new();

    public StringComparison Comparison { get; set; } = StringComparison.Ordinal;

    public bool Equals(Ngram a, Ngram b) =>
        b.Memory.Span.Equals(a.Memory.Span, Comparison);

    public int GetHashCode(Ngram o) =>
        string.GetHashCode(o.Memory.Span, Comparison);
}
