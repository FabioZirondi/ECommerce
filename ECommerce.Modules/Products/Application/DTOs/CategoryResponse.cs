using ECommerce.Modules.Products.Domain.Entities;

namespace ECommerce.Modules.Products.Application.DTOs;

public sealed class CategoryResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ParentId { get; init; }

    public static CategoryResponse From(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Slug = string.IsNullOrWhiteSpace(category.Slug) ? category.Id : category.Slug,
        ParentId = string.IsNullOrWhiteSpace(category.ParentId) ? null : category.ParentId
    };
}

public sealed class CategoryPathItem
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}
