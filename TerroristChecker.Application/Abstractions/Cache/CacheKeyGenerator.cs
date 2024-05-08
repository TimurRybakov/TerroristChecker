using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TerroristChecker.Application.Abstractions.Cache;

public static class CacheKeyGenerator<T>
{
    public static string Generate(T source)
    {
        var serializedSource = JsonSerializer.Serialize((object?)source, new JsonSerializerOptions { IncludeFields = true });
        byte[] serializedSourceBytes = Encoding.UTF8.GetBytes(serializedSource);
        byte[] hashBytes = MD5.HashData(serializedSourceBytes);
        var hashString = Encoding.UTF8.GetString(hashBytes);
        return $"{typeof(T)}-{hashString}";
    }
}
