using System.ComponentModel.DataAnnotations;
using PersonalLibrary.Application.Identity.Exceptions;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.Application.Identity.Commands.Login;

public sealed class LoginUserHandler(
    IIdentityService identityService,
    ITokenService tokenService)
    : ILoginHandler
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public async Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var email = command.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            !EmailValidator.IsValid(email) ||
            string.IsNullOrEmpty(command.Password))
        {
            throw new AuthenticationFlowException(
                "InvalidCredentials",
                "Invalid email or password.");
        }

        var authentication = await identityService.AuthenticateAsync(
            email,
            command.Password,
            cancellationToken);

        if (!authentication.Succeeded)
        {
            var error = authentication.Errors.FirstOrDefault()
                ?? new("InvalidCredentials", "Invalid email or password.");
            throw new AuthenticationFlowException(
                error.Code,
                error.Description,
                authentication.Errors);
        }

        var tokens = await tokenService.IssueAsync(authentication, cancellationToken);

        return new LoginUserResult(
            authentication.UserId!.Value,
            authentication.Email!,
            authentication.Name!,
            tokens);
    }
}