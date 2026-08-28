namespace ECommerce.Shared.Extensions;

public static class DateTimeExtensions
{
    public static DateTime EnsureUtc(this DateTime value)
        => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
