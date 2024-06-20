using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TerroristChecker.Domain.Abstractions;

public class Result
{
    protected internal Result(Error? error = null)
    {
        IsSuccess = error is null;
        Error = error;
    }

    public bool IsSuccess { get; }

    [JsonIgnore]
    public bool IsFailure { get { return !IsSuccess; } }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Error? Error { get; }

    public static Result Success() => new();

    public static Result Failure(Error error) => error is null ? throw new ArgumentNullException(nameof(error)) : new(error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, error);

    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    [JsonConstructor]
    protected internal Result(TValue? value, Error? error = null)
        : base(error)
    {
        _value = value;
    }

    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static implicit operator Result<TValue>(TValue? value) => Create(value);
}
