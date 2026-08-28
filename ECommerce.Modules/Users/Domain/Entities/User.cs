using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ECommerce.Modules.Users.Domain.Entities;

public sealed class User
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [UserRoles.Buyer];
    public UserStatus Status { get; set; } = UserStatus.Active;
    public List<Address> Addresses { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
