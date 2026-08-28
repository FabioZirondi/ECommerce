namespace ECommerce.Modules.Users.Application.DTOs;

public sealed class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool AsSeller { get; set; }
}
