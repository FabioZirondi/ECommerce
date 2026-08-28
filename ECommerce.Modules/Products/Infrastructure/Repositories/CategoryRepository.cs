using ECommerce.Modules.Products.Domain.Entities;
using ECommerce.Modules.Products.Domain.Interfaces;
using MongoDB.Driver;

namespace ECommerce.Modules.Products.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IMongoCollection<Category> _collection;

    public CategoryRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Category>("categories");
    }

    public Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _collection.Find(category => category.Id == id).FirstOrDefaultAsync(cancellationToken)!;

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => _collection.Find(category => category.Slug == slug || category.Id == slug)
            .FirstOrDefaultAsync(cancellationToken)!;

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default)
        => await _collection.Find(FilterDefinition<Category>.Empty).ToListAsync(cancellationToken);

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        => _collection.InsertOneAsync(category, cancellationToken: cancellationToken);
}
