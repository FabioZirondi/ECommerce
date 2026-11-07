using ECommerce.API.Extensions;
using ECommerce.Modules.Orders.Application.DTOs;
using ECommerce.Modules.Orders.Application.Services;
using ECommerce.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public sealed class CartController : ControllerBase
{
    private readonly ICartService _carts;

    public CartController(ICartService carts)
    {
        _carts = carts;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _carts.GetAsync(User.GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _carts.AddItemAsync(User.GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("items/{productId}")]
    public async Task<IActionResult> SetQuantity(
        string productId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _carts.SetQuantityAsync(User.GetUserId(), productId, request.Quantity, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItem(string productId, CancellationToken cancellationToken)
    {
        var result = await _carts.RemoveItemAsync(User.GetUserId(), productId, cancellationToken);
        return result.ToActionResult();
    }
}
