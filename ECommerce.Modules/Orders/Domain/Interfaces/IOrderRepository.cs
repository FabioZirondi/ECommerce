using ECommerce.Modules.Orders.Domain.Entities;

namespace ECommerce.Modules.Orders.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListByBuyerAsync(string buyerId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListBySellerAsync(string sellerId, CancellationToken cancellationToken = default);
}

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpsertAsync(Cart cart, CancellationToken cancellationToken = default);
}
