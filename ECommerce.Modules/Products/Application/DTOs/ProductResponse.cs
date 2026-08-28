using ECommerce.Modules.Products.Domain.Entities;
using ECommerce.Modules.Users.Domain.Entities;

namespace ECommerce.Modules.Products.Application.DTOs;

public sealed class ProductResponse
{
    public string Id { get; init; } = string.Empty;
    public string SellerId { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string CategoryId { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public IReadOnlyList<CategoryPathItem> CategoryPath { get; init; } = [];
    public string Condition { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Neighborhood { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public IReadOnlyList<string> Images { get; init; } = [];
    public int AvailableUnits { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public static ProductResponse From(
        Product product,
        IReadOnlyList<CategoryPathItem>? path = null,
        Address? sellerAddress = null,
        bool includePreciseAddress = true)
    {
        var categoryPath = path ?? [];
        var neighborhood = FirstValue(product.Neighborhood, string.Empty);
        var street = FirstValue(product.Street, FormatStreet(sellerAddress));
        var zipCode = FirstValue(product.ZipCode, sellerAddress?.ZipCode);
        return new()
        {
            Id = product.Id,
            SellerId = product.SellerId,
            SellerName = product.SellerName,
            Title = product.Title,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            CategoryName = categoryPath.LastOrDefault()?.Name ?? string.Empty,
            CategoryPath = categoryPath,
            Condition = product.Condition.ToString(),
            City = FirstValue(product.City, sellerAddress?.City),
            State = FirstValue(product.State, sellerAddress?.State),
            Neighborhood = includePreciseAddress ? neighborhood : string.Empty,
            Street = includePreciseAddress ? street : string.Empty,
            ZipCode = includePreciseAddress ? zipCode : string.Empty,
            Images = product.Images,
            AvailableUnits = product.AvailableUnits,
            Status = product.Status.ToString(),
            CreatedAt = product.CreatedAt
        };
    }

    private static string FirstValue(string? preferred, string? fallback)
        => string.IsNullOrWhiteSpace(preferred) ? fallback?.Trim() ?? string.Empty : preferred.Trim();

    private static string FormatStreet(Address? address)
    {
        if (address is null)
        {
            return string.Empty;
        }

        return string.Join(", ", new[] { address.Street, address.Number, address.Complement }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
