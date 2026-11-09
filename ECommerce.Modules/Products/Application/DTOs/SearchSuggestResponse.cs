namespace ECommerce.Modules.Products.Application.DTOs;

public sealed class SearchSuggestResponse
{
    public IReadOnlyList<ProductSuggestItem> Products { get; init; } = [];
    public IReadOnlyList<PlaceSuggestItem> Places { get; init; } = [];
    public IReadOnlyList<ProductSuggestItem> Nearby { get; init; } = [];
    public IReadOnlyList<CategorySuggestItem> Categories { get; init; } = [];
}

public sealed class ProductSuggestItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string? Image { get; init; }
    public string? Hint { get; init; }
}

public sealed class PlaceSuggestItem
{
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public int Count { get; init; }
    public string Label { get; init; } = string.Empty;
}

public sealed class CategorySuggestItem
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}
