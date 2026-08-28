using ECommerce.Modules.Users.Domain.Entities;

namespace ECommerce.Modules.Users.Application.DTOs;

public sealed class UserResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public static UserResponse From(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Roles = user.Roles,
        Status = user.Status.ToString(),
        CreatedAt = user.CreatedAt
    };
}
