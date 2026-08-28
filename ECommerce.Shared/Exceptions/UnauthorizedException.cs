namespace ECommerce.Shared.Exceptions;

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Não autorizado.") : base(message, 401)
    {
    }
}
