namespace ECommerce.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsBlank(this string? value) => string.IsNullOrWhiteSpace(value);

    public static string NormalizeEmail(this string email) => email.Trim().ToLowerInvariant();
}
