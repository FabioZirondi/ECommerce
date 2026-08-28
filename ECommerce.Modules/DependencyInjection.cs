using ECommerce.Modules.Orders.Application.Services;
using ECommerce.Modules.Orders.Domain.Interfaces;
using ECommerce.Modules.Orders.Infrastructure.Repositories;
using ECommerce.Modules.Products.Application.Services;
using ECommerce.Modules.Products.Domain.Interfaces;
using ECommerce.Modules.Products.Infrastructure.Repositories;
using ECommerce.Modules.Users.Application.Options;
using ECommerce.Modules.Users.Application.Security;
using ECommerce.Modules.Users.Application.Services;
using ECommerce.Modules.Users.Application.Validators;
using ECommerce.Modules.Users.Domain.Interfaces;
using ECommerce.Modules.Users.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Modules;

public static class DependencyInjection
{
    public static IServiceCollection AddECommerceModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductCatalog, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICartService, CartService>();

        return services;
    }
}
