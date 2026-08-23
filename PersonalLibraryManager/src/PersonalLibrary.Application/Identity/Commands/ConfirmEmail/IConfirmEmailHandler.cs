namespace PersonalLibrary.Application.Identity.Commands.ConfirmEmail;

public interface IConfirmEmailHandler
{
    Task HandleAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default);
}
