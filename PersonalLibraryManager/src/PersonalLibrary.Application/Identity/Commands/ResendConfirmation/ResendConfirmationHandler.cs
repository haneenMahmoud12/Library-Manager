using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.Application.Identity.Commands.ResendConfirmation;

public sealed class ResendConfirmationHandler(
    IIdentityService identityService,
    IEmailService emailService)
    : IResendConfirmationHandler
{
    public async Task HandleAsync(
        ResendConfirmationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Email))
            return;

        var challenge = await identityService.CreateEmailConfirmationChallengeAsync(
            command.Email.Trim(),
            cancellationToken);

        // A generic success response prevents account enumeration.
        if (challenge is not null)
            await emailService.SendConfirmationEmailAsync(challenge, cancellationToken);
    }
}
