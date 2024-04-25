using TerroristChecker.Domain.Abstractions;

namespace TerroristChecker.Domain;

public static class TerroristErrors
{
    public static readonly Error NotAcquired = new(
        "Terrorists.NotAcquired",
        "Can`t get terrorists from database");
}
