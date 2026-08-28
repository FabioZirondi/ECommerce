using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ECommerce.Modules.Products.Domain.Entities;

public sealed class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [BsonRepresentation(BsonType.String)]
    public string SellerId { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    [BsonRepresentation(BsonType.String)]
    public string CategoryId { get; set; } = string.Empty;

    public ProductCondition Condition { get; set; } = ProductCondition.Used;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];

    [BsonDefaultValue(1)]
    public int AvailableUnits { get; set; } = 1;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
