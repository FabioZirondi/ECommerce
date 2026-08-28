namespace ECommerce.Shared.Results;

public class Result
{
    public bool IsSuccess { get; init; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; init; }
    public int StatusCode { get; init; } = 200;
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public static Result Success() => new() { IsSuccess = true };

    public static Result Failure(
        string error,
        int statusCode = 400,
        IReadOnlyDictionary<string, string[]>? errors = null)
        => new()
        {
            IsSuccess = false,
            Error = error,
            StatusCode = statusCode,
            Errors = errors
        };
}

public class Result<T> : Result
{
    public T? Value { get; init; }

    public static Result<T> Success(T value, int statusCode = 200)
        => new()
        {
            IsSuccess = true,
            Value = value,
            StatusCode = statusCode
        };

    public static new Result<T> Failure(
        string error,
        int statusCode = 400,
        IReadOnlyDictionary<string, string[]>? errors = null)
        => new()
        {
            IsSuccess = false,
            Error = error,
            StatusCode = statusCode,
            Errors = errors
        };
}
