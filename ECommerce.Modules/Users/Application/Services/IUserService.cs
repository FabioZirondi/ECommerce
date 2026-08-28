using ECommerce.Modules.Users.Application.DTOs;
using ECommerce.Shared.Results;

namespace ECommerce.Modules.Users.Application.Services;

public interface IUserService
{
    Task<Result<AuthResponse>> RegisterAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserResponse>> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
