using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;
using ApplicationIdentityError = PersonalLibrary.Application.Identity.Models.IdentityError;

namespace PersonalLibrary.Infrastructure.Identity.Services;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
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
            return CreationFailure(new("DuplicateEmail", "An account with this email already exists."));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = name,
            CreatedAtUtc = DateTime.UtcNow,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, password);
        cancellationToken.ThrowIfCancellationRequested();

        if (!result.Succeeded)
        {
            return new CreateIdentityUserResult(
                null,
                null,
                null,
                MapErrors(result.Errors));
        }

        return new CreateIdentityUserResult(
            user.Id,
            user.Email,
            user.DisplayName,
            []);
    }

    public async Task<AuthenticatedUserResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return AuthenticationFailure("InvalidCredentials", "Invalid email or password.");

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);
        cancellationToken.ThrowIfCancellationRequested();

        if (signInResult.IsLockedOut)
            return AuthenticationFailure("LockedOut", "The account is temporarily locked.");

        if (!signInResult.Succeeded)
        {
            // Reveal confirmation status only when the supplied password is valid.
            if (!user.EmailConfirmed && await userManager.CheckPasswordAsync(user, password))
            {
                return AuthenticationFailure(
                    "EmailNotConfirmed",
                    "Confirm your email before signing in.");
            }

            return AuthenticationFailure("InvalidCredentials", "Invalid email or password.");
        }

        if (!user.EmailConfirmed)
            return AuthenticationFailure("EmailNotConfirmed", "Confirm your email before signing in.");

        var roles = await userManager.GetRolesAsync(user);
        return new AuthenticatedUserResult(
            user.Id,
            user.Email,
            user.DisplayName,
            roles.ToArray(),
            []);
    }

    public async Task<EmailConfirmationChallenge?> CreateEmailConfirmationChallengeAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.EmailConfirmed || user.Email is null)
            return null;

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return new EmailConfirmationChallenge(
            user.Id,
            user.Email,
            user.DisplayName,
            encodedToken);
    }

    public async Task<IdentityOperationResult> ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return OperationFailure(new("InvalidConfirmation", "Invalid confirmation link."));

        if (user.EmailConfirmed)
            return new IdentityOperationResult(true, []);

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return OperationFailure(new("InvalidConfirmation", "Invalid confirmation token."));
        }

        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        return result.Succeeded
            ? new IdentityOperationResult(true, [])
            : new IdentityOperationResult(false, MapErrors(result.Errors));
    }

    private static CreateIdentityUserResult CreationFailure(ApplicationIdentityError error) =>
        new(null, null, null, [error]);

    private static AuthenticatedUserResult AuthenticationFailure(
        string code,
        string description) =>
        new(null, null, null, [], [new(code, description)]);

    private static IdentityOperationResult OperationFailure(ApplicationIdentityError error) =>
        new(false, [error]);

    private static ApplicationIdentityError[] MapErrors(IEnumerable<Microsoft.AspNetCore.Identity.IdentityError> errors) =>
        errors.Select(error => new ApplicationIdentityError(error.Code, error.Description)).ToArray();
}