using ECommerce.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.StatusCode switch
            {
                201 => new ObjectResult(result.Value) { StatusCode = 201 },
                204 => new NoContentResult(),
                _ => new OkObjectResult(result.Value)
            };
        }

        return new ObjectResult(new { message = result.Error, errors = result.Errors })
        {
            StatusCode = result.StatusCode
        };
    }
}
