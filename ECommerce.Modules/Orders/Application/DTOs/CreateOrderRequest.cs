namespace ECommerce.Modules.Orders.Application.DTOs;

public sealed class CreateOrderRequest
{
    public string? CartId { get; set; }
}

public sealed class AddCartItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

public sealed class OrderItemResponse
{
    public string ProductId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

public sealed class OrderResponse
{
    public string Id { get; init; } = string.Empty;
    public string BuyerId { get; init; } = string.Empty;
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerEmail { get; init; } = string.Empty;
    public string SellerId { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<OrderItemResponse> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
