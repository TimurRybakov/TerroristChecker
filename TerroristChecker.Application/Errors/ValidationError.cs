namespace TerroristChecker.Application.Errors;

public sealed record ValidationError(string PropertyName, string ErrorMessage);
