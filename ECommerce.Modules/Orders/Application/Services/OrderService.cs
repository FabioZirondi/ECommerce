using ECommerce.Modules.Orders.Application.DTOs;
using ECommerce.Modules.Orders.Domain.Entities;
using ECommerce.Modules.Orders.Domain.Interfaces;
using ECommerce.Modules.Products.Domain.Entities;
using ECommerce.Modules.Products.Domain.Interfaces;
using ECommerce.Modules.Users.Domain.Interfaces;
using ECommerce.Shared.Results;

namespace ECommerce.Modules.Orders.Application.Services;

public interface IOrderService
{
    Task<Result<IReadOnlyList<OrderResponse>>> ListMineAsync(string buyerId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<OrderResponse>>> ListSalesAsync(string sellerId, CancellationToken cancellationToken = default);
    Task<Result<OrderResponse>> GetByIdAsync(string id, string userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<OrderResponse>>> CheckoutAsync(string buyerId, CancellationToken cancellationToken = default);
}

public interface ICartService
{
    Task<Result<Cart>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<Cart>> AddItemAsync(string userId, AddCartItemRequest request, CancellationToken cancellationToken = default);
}

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly IUserRepository _users;

    public OrderService(
        IOrderRepository orders,
        ICartRepository carts,
        IProductRepository products,
        IUserRepository users)
    {
        _orders = orders;
        _carts = carts;
        _products = products;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<OrderResponse>>> ListMineAsync(
        string buyerId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orders.ListByBuyerAsync(buyerId, cancellationToken);
        var mapped = new List<OrderResponse>();
        foreach (var order in orders)
        {
            mapped.Add(await ToResponseAsync(order, cancellationToken));
        }

        return Result<IReadOnlyList<OrderResponse>>.Success(mapped);
    }

    public async Task<Result<IReadOnlyList<OrderResponse>>> ListSalesAsync(
        string sellerId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orders.ListBySellerAsync(sellerId, cancellationToken);
        var mapped = new List<OrderResponse>();
        foreach (var order in orders)
        {
            mapped.Add(await ToResponseAsync(order, cancellationToken));
        }

        return Result<IReadOnlyList<OrderResponse>>.Success(mapped);
    }

    public async Task<Result<OrderResponse>> GetByIdAsync(
        string id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null || (order.BuyerId != userId && order.SellerId != userId))
        {
            return Result<OrderResponse>.Failure("Pedido não encontrado.", 404);
        }

        return Result<OrderResponse>.Success(await ToResponseAsync(order, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<OrderResponse>>> CheckoutAsync(
        string buyerId,
        CancellationToken cancellationToken = default)
    {
        var buyer = await _users.GetByIdAsync(buyerId, cancellationToken);
        if (buyer is null)
        {
            return Result<IReadOnlyList<OrderResponse>>.Failure("Usuário não encontrado.", 401);
        }

        var cart = await _carts.GetByUserIdAsync(buyerId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return Result<IReadOnlyList<OrderResponse>>.Failure("Seu carrinho está vazio.");
        }

        var lines = new List<(CartItem Item, Product Product)>();
        foreach (var item in cart.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null || product.Status != ProductStatus.Active || product.AvailableUnits < item.Quantity)
            {
                return Result<IReadOnlyList<OrderResponse>>.Failure(
                    $"O anúncio “{product?.Title ?? item.ProductId}” não tem unidades suficientes.");
            }

            if (string.Equals(product.SellerId, buyerId, StringComparison.Ordinal))
            {
                return Result<IReadOnlyList<OrderResponse>>.Failure("Você não pode comprar o próprio anúncio.");
            }

            lines.Add((item, product));
        }

        var created = new List<Order>();
        foreach (var group in lines.GroupBy(line => line.Product.SellerId))
        {
            var orderItems = group.Select(line => new OrderItem
            {
                ProductId = line.Product.Id,
                Title = line.Product.Title,
                Image = CoverImage(line.Product),
                UnitPrice = line.Product.Price,
                Quantity = line.Item.Quantity
            }).ToList();

            created.Add(new Order
            {
                BuyerId = buyer.Id,
                BuyerName = buyer.Name,
                BuyerEmail = buyer.Email,
                SellerId = group.Key,
                SellerName = group.First().Product.SellerName,
                Items = orderItems,
                Total = orderItems.Sum(item => item.UnitPrice * item.Quantity),
                Status = OrderStatus.Paid,
                PaidAt = DateTime.UtcNow
            });
        }

        foreach (var (item, product) in lines)
        {
            product.AvailableUnits -= item.Quantity;
            if (product.AvailableUnits <= 0)
            {
                product.AvailableUnits = 0;
                product.Status = ProductStatus.Sold;
            }

            await _products.ReplaceAsync(product, cancellationToken);
        }

        foreach (var order in created)
        {
            await _orders.AddAsync(order, cancellationToken);
        }

        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _carts.UpsertAsync(cart, cancellationToken);

        var mapped = new List<OrderResponse>();
        foreach (var order in created)
        {
            mapped.Add(await ToResponseAsync(order, cancellationToken));
        }

        return Result<IReadOnlyList<OrderResponse>>.Success(mapped, 201);
    }

    private async Task<OrderResponse> ToResponseAsync(Order order, CancellationToken cancellationToken)
    {
        var items = new List<OrderItemResponse>();
        foreach (var item in order.Items)
        {
            var image = item.Image;
            if (string.IsNullOrWhiteSpace(image))
            {
                var product = await _products.GetByIdAsync(item.ProductId, cancellationToken);
                image = CoverImage(product);
            }

            items.Add(new OrderItemResponse
            {
                ProductId = item.ProductId,
                Title = item.Title,
                Image = image,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            });
        }

        return new OrderResponse
        {
            Id = order.Id,
            BuyerId = order.BuyerId,
            BuyerName = order.BuyerName,
            BuyerEmail = order.BuyerEmail,
            SellerId = order.SellerId,
            SellerName = order.SellerName,
            Total = order.Total,
            Status = order.Status == OrderStatus.Paid ? "Purchased" : order.Status.ToString(),
            Items = items,
            CreatedAt = order.CreatedAt
        };
    }

    private static string CoverImage(Product? product)
        => product?.Images.FirstOrDefault(url => !string.IsNullOrWhiteSpace(url)) ?? string.Empty;
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

    public async Task<Result<Cart>> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken)
                   ?? new Cart { UserId = userId };
        return Result<Cart>.Success(cart);
    }

    public async Task<Result<Cart>> AddItemAsync(
        string userId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductId) || request.Quantity < 1)
        {
            return Result<Cart>.Failure("Item inválido.");
        }

        var product = await _catalog.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || product.Status != ProductStatus.Active || product.AvailableUnits < 1)
        {
            return Result<Cart>.Failure("Este anúncio não está disponível.");
        }

        if (string.Equals(product.SellerId, userId, StringComparison.Ordinal))
        {
            return Result<Cart>.Failure("Você não pode comprar o próprio anúncio.");
        }

        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken)
                   ?? new Cart { UserId = userId };

        var existing = cart.Items.FirstOrDefault(item => item.ProductId == request.ProductId);
        var desired = (existing?.Quantity ?? 0) + request.Quantity;
        if (desired > product.AvailableUnits)
        {
            return Result<Cart>.Failure(
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
            existing.Quantity += request.Quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _carts.UpsertAsync(cart, cancellationToken);
        return Result<Cart>.Success(cart);
    }
}
