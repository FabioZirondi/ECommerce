using ECommerce.API.Extensions;
using ECommerce.Modules.Orders.Application.DTOs;
using ECommerce.Modules.Orders.Application.Services;
using ECommerce.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly ICartService _carts;

    public OrdersController(IOrderService orders, ICartService carts)
    {
        _orders = orders;
        _carts = carts;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var result = await _orders.ListMineAsync(User.GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales(CancellationToken cancellationToken)
    {
        var result = await _orders.ListSalesAsync(User.GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        var result = await _orders.CheckoutAsync(User.GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _orders.GetByIdAsync(id, User.GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("/api/cart")]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var result = await _carts.GetAsync(User.GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("/api/cart/items")]
    public async Task<IActionResult> AddCartItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _carts.AddItemAsync(User.GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }
}
