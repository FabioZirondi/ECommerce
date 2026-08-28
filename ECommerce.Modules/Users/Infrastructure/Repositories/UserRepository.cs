using ECommerce.Modules.Users.Domain.Entities;
using ECommerce.Modules.Users.Domain.Interfaces;
using MongoDB.Driver;

namespace ECommerce.Modules.Users.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _collection;

    public UserRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<User>("users");
    }

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _collection.Find(user => user.Id == id).FirstOrDefaultAsync(cancellationToken)!;

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _collection.Find(user => user.Email == email).FirstOrDefaultAsync(cancellationToken)!;

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
        => _collection.InsertOneAsync(user, cancellationToken: cancellationToken);
}
