using ECommerce.Modules.Products.Domain.Entities;

namespace ECommerce.Modules.Products.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> ListAsync(
        int skip,
        int take,
        IReadOnlyCollection<string>? categoryIds = null,
        string? sellerId = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
    Task<long> CountAsync(
        IReadOnlyCollection<string>? categoryIds = null,
        string? sellerId = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task ReplaceAsync(Product product, CancellationToken cancellationToken = default);
}
