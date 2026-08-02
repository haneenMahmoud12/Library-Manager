using Microsoft.AspNetCore.Identity;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;
using ApplicationIdentityError = PersonalLibrary.Application.Identity.Models.IdentityError;

namespace PersonalLibrary.Infrastructure.Identity.Services;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager)
    : IIdentityService
{
    public async Task<CreateIdentityUserResult> CreateUserAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return Failure(new("DuplicateEmail", "An account with this email already exists."));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = name,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        cancellationToken.ThrowIfCancellationRequested();

        if (!result.Succeeded)
        {
            return new CreateIdentityUserResult(
                null,
                null,
                null,
                result.Errors
                    .Select(error => new ApplicationIdentityError(error.Code, error.Description))
                    .ToArray());
        }

        return new CreateIdentityUserResult(
            user.Id,
            user.Email,
            user.DisplayName,
            []);
    }

    private static CreateIdentityUserResult Failure(ApplicationIdentityError error) =>
        new(null, null, null, [error]);
}