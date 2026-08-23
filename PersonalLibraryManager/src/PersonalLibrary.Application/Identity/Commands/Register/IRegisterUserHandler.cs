namespace PersonalLibrary.Application.Identity.Commands.Register;

public interface IRegisterUserHandler
{
    Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default);
}
