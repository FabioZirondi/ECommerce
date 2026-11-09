using ECommerce.Modules.Users.Domain.Entities;

namespace ECommerce.Modules.Users.Application.DTOs;

public sealed class UserResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
    public string Status { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public static UserResponse From(User user)
    {
        var address = user.Addresses.FirstOrDefault();
        return new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Roles = user.Roles,
            Status = user.Status.ToString(),
            City = address?.City?.Trim() ?? string.Empty,
            State = address?.State?.Trim() ?? string.Empty,
            ZipCode = address?.ZipCode?.Trim() ?? string.Empty,
            CreatedAt = user.CreatedAt
        };
    }
}
