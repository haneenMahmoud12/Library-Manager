namespace PersonalLibrary.Application.Identity.Commands.ResendConfirmation;

public interface IResendConfirmationHandler
{
    Task HandleAsync(
        ResendConfirmationCommand command,
        CancellationToken cancellationToken = default);
}
