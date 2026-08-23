using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Services;

public interface IIdentityService
{
    Task<CreateIdentityUserResult> CreateUserAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedUserResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<EmailConfirmationChallenge?> CreateEmailConfirmationChallengeAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default);
}