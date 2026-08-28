using ECommerce.Modules.Orders.Domain.Entities;
using ECommerce.Modules.Orders.Domain.Interfaces;
using MongoDB.Driver;

namespace ECommerce.Modules.Orders.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly IMongoCollection<Order> _collection;

    public OrderRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Order>("orders");
    }

    public Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _collection.Find(order => order.Id == id).FirstOrDefaultAsync(cancellationToken)!;

    public async Task<IReadOnlyList<Order>> ListByBuyerAsync(string buyerId, CancellationToken cancellationToken = default)
        => await _collection.Find(order => order.BuyerId == buyerId)
            .SortByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        => _collection.InsertOneAsync(order, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<Order>> ListBySellerAsync(string sellerId, CancellationToken cancellationToken = default)
        => await _collection.Find(order => order.SellerId == sellerId)
            .SortByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
}

public sealed class CartRepository : ICartRepository
{
    private readonly IMongoCollection<Cart> _collection;

    public CartRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Cart>("carts");
    }

    public Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => _collection.Find(cart => cart.UserId == userId).FirstOrDefaultAsync(cancellationToken)!;

    public Task UpsertAsync(Cart cart, CancellationToken cancellationToken = default)
        => _collection.ReplaceOneAsync(
            existing => existing.UserId == cart.UserId,
            cart,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
}
