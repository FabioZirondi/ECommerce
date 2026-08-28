using ECommerce.Modules.Products.Domain.Entities;

namespace ECommerce.Modules.Products.Domain.Interfaces;

public interface IProductCatalog
{
    Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(string id, CancellationToken cancellationToken = default);
}
