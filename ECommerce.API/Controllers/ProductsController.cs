using ECommerce.API.Extensions;
using ECommerce.Modules.Products.Application.DTOs;
using ECommerce.Modules.Products.Application.Services;
using ECommerce.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products)
    {
        _products = products;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _products.ListAsync(page, pageSize, category, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _products.ListMineAsync(User.GetUserId(), page, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _products.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var name = User.Identity?.Name ?? "Vendedor";
        var result = await _products.CreateAsync(User.GetUserId(), name, request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _products.UpdateAsync(User.GetUserId(), id, request, cancellationToken);
        return result.ToActionResult();
    }
}
