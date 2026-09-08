using System.Security.Claims;
using PersonalLibrary.Application.Common.Authentication;

namespace PersonalLibrary.Api.Authentication;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext
{
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var value = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal?.FindFirst("sub")?.Value;

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
