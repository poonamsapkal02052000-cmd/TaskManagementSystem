using System.Security.Claims;
using TaskManager.Api.Models;

namespace TaskManager.Api.Common;

public record CurrentUser(int Id, UserRole Role);

public static class ClaimsPrincipalExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? throw new UnauthorizedException("Invalid token.");
        var role = principal.FindFirstValue(ClaimTypes.Role)
                   ?? throw new UnauthorizedException("Invalid token.");
        return new CurrentUser(int.Parse(id), Enum.Parse<UserRole>(role));
    }
}
