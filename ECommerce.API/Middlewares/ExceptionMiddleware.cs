using System.Net;
using System.Text.Json;
using ECommerce.Shared.Exceptions;

namespace ECommerce.API.Middlewares;

public sealed class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteAsync(context, exception);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        var status = HttpStatusCode.InternalServerError;
        object body = new { message = "Erro interno." };

        switch (exception)
        {
            case ValidationException validation:
                status = HttpStatusCode.BadRequest;
                body = new { message = validation.Message, errors = validation.Errors };
                break;
            case AppException app:
                status = (HttpStatusCode)app.StatusCode;
                body = new { message = app.Message };
                break;
            default:
                _logger.LogError(exception, "Unhandled exception");
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
