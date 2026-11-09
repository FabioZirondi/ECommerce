using ECommerce.Modules.Orders.Domain.Entities;

namespace ECommerce.Modules.Orders.Application.DTOs;

public sealed class AddCartItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

public sealed class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}

public sealed class CartItemResponse
{
    public string ProductId { get; init; } = string.Empty;
    public string SellerId { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public sealed class CartResponse
{
    public string UserId { get; init; } = string.Empty;
    public IReadOnlyList<CartItemResponse> Items { get; init; } = [];
    public DateTime UpdatedAt { get; init; }

    public static CartResponse From(Cart cart) => new()
    {
        UserId = cart.UserId,
        Items = cart.Items.Select(item => new CartItemResponse
        {
            ProductId = item.ProductId,
            SellerId = item.SellerId,
            Quantity = item.Quantity
        }).ToList(),
        UpdatedAt = cart.UpdatedAt
    };
}

public sealed class OrderItemResponse
{
    public string ProductId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

public sealed class CheckoutRequest
{
    public string PaymentMethod { get; set; } = string.Empty;
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
    public string? PaymentMethod { get; init; }
    public IReadOnlyList<OrderItemResponse> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
