using PersonalLibrary.Application.Identity.Exceptions;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.Application.Identity.Commands.ConfirmEmail;

public sealed class ConfirmEmailHandler(IIdentityService identityService)
{
    public async Task HandleAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UserId == Guid.Empty || string.IsNullOrWhiteSpace(command.Token))
            throw new AuthenticationFlowException("InvalidConfirmation", "Invalid confirmation link.");

        var result = await identityService.ConfirmEmailAsync(
            command.UserId,
            command.Token,
            cancellationToken);

        if (!result.Succeeded)
            throw new AuthenticationFlowException(
                "InvalidConfirmation",
                "The confirmation link is invalid or expired.",
                result.Errors);
    }
}
