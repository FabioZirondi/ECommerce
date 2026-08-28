using ECommerce.Modules.Products.Domain.Entities;
using ECommerce.Modules.Products.Domain.Interfaces;
using MongoDB.Driver;

namespace ECommerce.Modules.Products.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository, IProductCatalog
{
    private readonly IMongoCollection<Product> _collection;

    public ProductRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Product>("products");
    }

    public Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _collection.Find(product => product.Id == id).FirstOrDefaultAsync(cancellationToken)!;

    public async Task<IReadOnlyList<Product>> ListAsync(
        int skip,
        int take,
        IReadOnlyCollection<string>? categoryIds = null,
        string? sellerId = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
        => await _collection.Find(BuildFilter(categoryIds, sellerId, activeOnly))
            .Skip(skip)
            .Limit(take)
            .SortByDescending(product => product.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<long> CountAsync(
        IReadOnlyCollection<string>? categoryIds = null,
        string? sellerId = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
        => _collection.CountDocumentsAsync(BuildFilter(categoryIds, sellerId, activeOnly), cancellationToken: cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        => _collection.InsertOneAsync(product, cancellationToken: cancellationToken);

    public Task ReplaceAsync(Product product, CancellationToken cancellationToken = default)
        => _collection.ReplaceOneAsync(item => item.Id == product.Id, product, cancellationToken: cancellationToken);

    public async Task<bool> IsAvailableAsync(string id, CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);
        return product is { Status: ProductStatus.Active, AvailableUnits: > 0 };
    }

    private static FilterDefinition<Product> BuildFilter(
        IReadOnlyCollection<string>? categoryIds,
        string? sellerId,
        bool activeOnly)
    {
        var filters = new List<FilterDefinition<Product>>();

        if (categoryIds is not null)
        {
            filters.Add(categoryIds.Count == 0
                ? Builders<Product>.Filter.Eq(product => product.Id, "__none__")
                : Builders<Product>.Filter.In(product => product.CategoryId, categoryIds));
        }

        if (!string.IsNullOrWhiteSpace(sellerId))
        {
            filters.Add(Builders<Product>.Filter.Eq(product => product.SellerId, sellerId));
        }

        if (activeOnly)
        {
            filters.Add(Builders<Product>.Filter.Eq(product => product.Status, ProductStatus.Active));
            filters.Add(Builders<Product>.Filter.Gt(product => product.AvailableUnits, 0));
        }

        return filters.Count == 0
            ? FilterDefinition<Product>.Empty
            : Builders<Product>.Filter.And(filters);
    }
}
