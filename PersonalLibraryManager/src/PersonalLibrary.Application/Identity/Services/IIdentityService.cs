using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Services;

public interface IIdentityService
{
    Task<CreateIdentityUserResult> CreateUserAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken = default);
}