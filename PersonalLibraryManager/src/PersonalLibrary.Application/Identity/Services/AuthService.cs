using System.ComponentModel.DataAnnotations;
using PersonalLibrary.Application.Identity.Exceptions;
using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Services;

public sealed class AuthService(
    IIdentityService identityService,
    ITokenService tokenService,
    IEmailService emailService) : IAuthService
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public async Task<RegisterViewModel> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var email = dto.Email?.Trim();
        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > 320 ||
            !EmailValidator.IsValid(email))
        {
            throw new RegistrationValidationException(
                [new("InvalidEmail", "A valid email address is required.")]);
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
        {
            throw new RegistrationValidationException(
                [new("InvalidName", "Name is required and cannot exceed 150 characters.")]);
        }

        if (string.IsNullOrEmpty(dto.Password) || dto.Password.Length > 128)
        {
            throw new RegistrationValidationException(
                [new("InvalidPassword", "Password is required and cannot exceed 128 characters.")]);
        }

        if (dto.Password != dto.ConfirmPassword)
        {
            throw new RegistrationValidationException(
                [new("PasswordMismatch", "Password and confirmation do not match.")]);
        }

        var creation = await identityService.CreateUserAsync(
            email,
            dto.Password,
            name,
            cancellationToken);

        if (!creation.Succeeded)
            throw new RegistrationValidationException(creation.Errors);

        var challenge = await identityService.CreateEmailConfirmationChallengeAsync(
            creation.Email!,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "The email confirmation challenge could not be created.");

        await emailService.SendConfirmationEmailAsync(challenge, cancellationToken);

        return new RegisterViewModel(
            creation.UserId!.Value,
            creation.Email!,
            creation.Name!);
    }

    public async Task<LoginViewModel> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var email = dto.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            !EmailValidator.IsValid(email) ||
            string.IsNullOrEmpty(dto.Password))
        {
            throw new AuthenticationFlowException(
                "InvalidCredentials",
                "Invalid email or password.");
        }

        var authentication = await identityService.AuthenticateAsync(
            email,
            dto.Password,
            cancellationToken);

        if (!authentication.Succeeded)
        {
            var error = authentication.Errors.FirstOrDefault()
                ?? new IdentityError("InvalidCredentials", "Invalid email or password.");
            throw new AuthenticationFlowException(
                error.Code,
                error.Description,
                authentication.Errors);
        }

        var tokens = await tokenService.IssueAsync(authentication, cancellationToken);

        return new LoginViewModel(
            authentication.UserId!.Value,
            authentication.Email!,
            authentication.Name!,
            tokens);
    }

    public async Task ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            throw new AuthenticationFlowException("InvalidConfirmation", "Invalid confirmation link.");

        var result = await identityService.ConfirmEmailAsync(userId, token, cancellationToken);
        if (!result.Succeeded)
        {
            throw new AuthenticationFlowException(
                "InvalidConfirmation",
                "The confirmation link is invalid or expired.",
                result.Errors);
        }
    }

    public async Task ResendConfirmationAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var challenge = await identityService.CreateEmailConfirmationChallengeAsync(
            email.Trim(),
            cancellationToken);

        // A generic API response prevents account enumeration.
        if (challenge is not null)
            await emailService.SendConfirmationEmailAsync(challenge, cancellationToken);
    }

    public async Task<TokenPairResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new AuthenticationFlowException("InvalidRefreshToken", "Invalid refresh token.");

        return await tokenService.RefreshAsync(refreshToken, cancellationToken)
            ?? throw new AuthenticationFlowException(
                "InvalidRefreshToken",
                "Invalid or expired refresh token.");
    }
}
