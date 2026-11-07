using ECommerce.Modules.Orders.Application.DTOs;
using ECommerce.Modules.Orders.Domain.Entities;
using ECommerce.Modules.Orders.Domain.Interfaces;
using ECommerce.Modules.Products.Domain.Entities;
using ECommerce.Modules.Products.Domain.Interfaces;
using ECommerce.Shared.Results;

namespace ECommerce.Modules.Orders.Application.Services;

public interface ICartService
{
    Task<Result<CartResponse>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> AddItemAsync(string userId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> SetQuantityAsync(string userId, string productId, int quantity, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> RemoveItemAsync(string userId, string productId, CancellationToken cancellationToken = default);
}

public sealed class CartService : ICartService
{
    private readonly ICartRepository _carts;
    private readonly IProductCatalog _catalog;

    public CartService(ICartRepository carts, IProductCatalog catalog)
    {
        _carts = carts;
        _catalog = catalog;
    }

    public async Task<Result<CartResponse>> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken)
                   ?? new Cart { UserId = userId };
        return Result<CartResponse>.Success(CartResponse.From(cart));
    }

    public async Task<Result<CartResponse>> AddItemAsync(
        string userId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductId) || request.Quantity < 1)
        {
            return Result<CartResponse>.Failure("Item inválido.");
        }

        var product = await _catalog.GetByIdAsync(request.ProductId, cancellationToken);
        var availability = ValidatePurchase(product, userId);
        if (availability is not null)
        {
            return Result<CartResponse>.Failure(availability);
        }

        var cart = await LoadAsync(userId, cancellationToken);
        var existing = cart.Items.FirstOrDefault(item => item.ProductId == request.ProductId);
        var desired = (existing?.Quantity ?? 0) + request.Quantity;
        if (desired > product!.AvailableUnits)
        {
            return Result<CartResponse>.Failure(
                $"Só há {product.AvailableUnits} unidade(s) disponível(is).");
        }

        if (existing is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = request.ProductId,
                SellerId = product.SellerId,
                Quantity = request.Quantity
            });
        }
        else
        {
            existing.Quantity = desired;
        }

        return await SaveAsync(cart, cancellationToken);
    }

    public async Task<Result<CartResponse>> SetQuantityAsync(
        string userId,
        string productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return Result<CartResponse>.Failure("Item inválido.");
        }

        if (quantity < 1)
        {
            return await RemoveItemAsync(userId, productId, cancellationToken);
        }

        var cart = await LoadAsync(userId, cancellationToken);
        var existing = cart.Items.FirstOrDefault(item => item.ProductId == productId);
        if (existing is null)
        {
            return Result<CartResponse>.Failure("Item não está no carrinho.", 404);
        }

        var product = await _catalog.GetByIdAsync(productId, cancellationToken);
        var availability = ValidatePurchase(product, userId);
        if (availability is not null)
        {
            return Result<CartResponse>.Failure(availability);
        }

        if (quantity > product!.AvailableUnits)
        {
            return Result<CartResponse>.Failure(
                $"Só há {product.AvailableUnits} unidade(s) disponível(is).");
        }

        existing.Quantity = quantity;
        return await SaveAsync(cart, cancellationToken);
    }

    public async Task<Result<CartResponse>> RemoveItemAsync(
        string userId,
        string productId,
        CancellationToken cancellationToken = default)
    {
        var cart = await LoadAsync(userId, cancellationToken);
        var removed = cart.Items.RemoveAll(item => item.ProductId == productId);
        if (removed == 0)
        {
            return Result<CartResponse>.Success(CartResponse.From(cart));
        }

        return await SaveAsync(cart, cancellationToken);
    }

    private async Task<Cart> LoadAsync(string userId, CancellationToken cancellationToken)
        => await _carts.GetByUserIdAsync(userId, cancellationToken) ?? new Cart { UserId = userId };

    private async Task<Result<CartResponse>> SaveAsync(Cart cart, CancellationToken cancellationToken)
    {
        cart.UpdatedAt = DateTime.UtcNow;
        await _carts.UpsertAsync(cart, cancellationToken);
        return Result<CartResponse>.Success(CartResponse.From(cart));
    }

    private static string? ValidatePurchase(Product? product, string userId)
    {
        if (product is null || product.Status != ProductStatus.Active || product.AvailableUnits < 1)
        {
            return "Este anúncio não está disponível.";
        }

        if (string.Equals(product.SellerId, userId, StringComparison.Ordinal))
        {
            return "Você não pode comprar o próprio anúncio.";
        }

        return null;
    }
}
