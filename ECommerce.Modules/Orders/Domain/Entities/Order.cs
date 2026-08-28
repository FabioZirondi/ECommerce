using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ECommerce.Modules.Orders.Domain.Entities;

public sealed class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [BsonRepresentation(BsonType.String)]
    public string BuyerId { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;

    public List<OrderItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public sealed class OrderItem
{
    [BsonRepresentation(BsonType.String)]
    public string ProductId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public sealed class Cart
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [BsonRepresentation(BsonType.String)]
    public string UserId { get; set; } = string.Empty;

    public List<CartItem> Items { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CartItem
{
    [BsonRepresentation(BsonType.String)]
    public string ProductId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public string SellerId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
