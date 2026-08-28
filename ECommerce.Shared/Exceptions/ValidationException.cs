namespace ECommerce.Shared.Exceptions;

public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message, 400)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}
