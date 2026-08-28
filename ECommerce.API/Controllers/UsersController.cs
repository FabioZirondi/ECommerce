using ECommerce.API.Extensions;
using ECommerce.Modules.Users.Application.Services;
using ECommerce.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _users.GetByIdAsync(User.GetUserId(), cancellationToken);
        return result.ToActionResult();
    }
}
