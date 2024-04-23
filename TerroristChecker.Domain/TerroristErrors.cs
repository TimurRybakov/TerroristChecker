using TerroristChecker.Domain.Abstractions;

namespace TerroristChecker.Domain;

public static class TerroristErrors
{
    public static Error NotAcquired = new(
        "Terrorists.NotAcquired",
        "Can`t get terrorists from database");
}
