namespace ECommerce.Modules.Users.Application.Options;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ECommerce";
    public string Audience { get; set; } = "ECommerce";
    public string Secret { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 120;
}
