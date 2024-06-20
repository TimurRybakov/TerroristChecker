namespace TerroristChecker.Domain.Abstractions;

public record Error(string Code, string Name)
{
    public static Error NullValue = new("Error.NullValue", "Null value was provided");
}
