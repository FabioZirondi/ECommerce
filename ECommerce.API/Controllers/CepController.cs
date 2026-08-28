using ECommerce.API.Extensions;
using ECommerce.Modules.Products.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("sensitive")]
[Route("api/cep")]
public sealed class CepController : ControllerBase
{
    private readonly ICepLookupService _cep;

    public CepController(ICepLookupService cep)
    {
        _cep = cep;
    }

    [HttpGet("{zipCode}")]
    public async Task<IActionResult> Lookup(string zipCode, CancellationToken cancellationToken)
    {
        var result = await _cep.LookupAsync(zipCode, cancellationToken);
        return result.ToActionResult();
    }
}
