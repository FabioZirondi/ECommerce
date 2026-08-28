namespace ECommerce.Modules.Products.Application.DTOs;

public sealed class UpdateProductRequest
{
    public int? AvailableUnits { get; set; }
    public string? Status { get; set; }
}
