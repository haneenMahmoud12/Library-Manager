using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Services;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(
        EmailConfirmationChallenge challenge,
        CancellationToken cancellationToken = default);
}
