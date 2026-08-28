using ECommerce.API.Extensions;
using ECommerce.API.Uploads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("sensitive")]
[Route("api/uploads")]
public sealed class UploadsController : ControllerBase
{
    private readonly IImageStorage _images;

    public UploadsController(IImageStorage images)
    {
        _images = images;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var result = await _images.SaveAsync(file, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return new ObjectResult(new { url = result.Value }) { StatusCode = result.StatusCode };
    }
}
