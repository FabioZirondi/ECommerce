namespace ECommerce.Modules.Users.Application.DTOs;

public sealed class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresInMinutes { get; init; }
    public UserResponse User { get; init; } = null!;
}
