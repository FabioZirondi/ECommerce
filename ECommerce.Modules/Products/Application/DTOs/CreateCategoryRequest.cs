namespace ECommerce.Modules.Products.Application.DTOs;

public sealed class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
}
