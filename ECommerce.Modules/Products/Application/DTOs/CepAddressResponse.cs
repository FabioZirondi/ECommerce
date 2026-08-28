namespace ECommerce.Modules.Products.Application.DTOs;

public sealed class CepAddressResponse
{
    public string ZipCode { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Neighborhood { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
}
