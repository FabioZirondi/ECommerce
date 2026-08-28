using ECommerce.Modules.Users.Application.DTOs;
using ECommerce.Modules.Users.Application.Options;
using ECommerce.Modules.Users.Application.Security;
using ECommerce.Modules.Users.Domain.Entities;
using ECommerce.Modules.Users.Domain.Interfaces;
using ECommerce.Shared.Extensions;
using ECommerce.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ECommerce.Modules.Users.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly JwtSettings _jwtSettings;

    public UserService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        IValidator<CreateUserRequest> createValidator,
        IValidator<LoginRequest> loginValidator,
        IOptions<JwtSettings> jwtSettings)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _createValidator = createValidator;
        _loginValidator = loginValidator;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthResponse>.Failure("Dados inválidos.", 400, ToErrors(validation));
        }

        var email = request.Email.NormalizeEmail();
        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return Result<AuthResponse>.Failure("E-mail já cadastrado.", 409);
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Roles = request.AsSeller
                ? [UserRoles.Buyer, UserRoles.Seller]
                : [UserRoles.Buyer]
        };

        try
        {
            await _users.AddAsync(user, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result<AuthResponse>.Failure("E-mail já cadastrado.", 409);
        }

        return Result<AuthResponse>.Success(BuildAuth(user), 201);
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthResponse>.Failure("Dados inválidos.", 400, ToErrors(validation));
        }

        var user = await _users.GetByEmailAsync(request.Email.NormalizeEmail(), cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure("E-mail ou senha inválidos.", 401);
        }

        if (user.Status != UserStatus.Active)
        {
            return Result<AuthResponse>.Failure("Usuário inativo.", 403);
        }

        return Result<AuthResponse>.Success(BuildAuth(user));
    }

    public async Task<Result<UserResponse>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure("Usuário não encontrado.", 404);
        }

        return Result<UserResponse>.Success(UserResponse.From(user));
    }

    private AuthResponse BuildAuth(User user) => new()
    {
        AccessToken = _jwt.CreateToken(user),
        ExpiresInMinutes = _jwtSettings.ExpirationMinutes,
        User = UserResponse.From(user)
    };

    private static IReadOnlyDictionary<string, string[]> ToErrors(FluentValidation.Results.ValidationResult validation)
        => validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
}
