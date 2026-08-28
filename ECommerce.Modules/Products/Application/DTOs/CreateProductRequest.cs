namespace ECommerce.Modules.Products.Application.DTOs;

public sealed class CreateProductRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string Condition { get; set; } = "Used";
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public int AvailableUnits { get; set; } = 1;
    public List<string> Images { get; set; } = [];
}
