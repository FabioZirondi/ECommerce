using System.Security.Claims;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId.IsBlank())
        {
            throw new UnauthorizedException("Token inválido.");
        }

        return userId!;
    }
}
