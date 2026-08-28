using ECommerce.Modules.Users.Domain.Entities;

namespace ECommerce.Modules.Users.Application.Security;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
