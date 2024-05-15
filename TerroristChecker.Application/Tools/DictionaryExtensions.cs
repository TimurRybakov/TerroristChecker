using System.Runtime.InteropServices;

namespace TerroristChecker.Application.Tools;

public static class DictionaryExtensions
{
    public static ref TValue? GetValueRefOrAddDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
    {
        return ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out _);
    }

}
