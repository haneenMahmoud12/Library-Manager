namespace PersonalLibrary.Application.Identity.Commands;

public interface IRegisterUserHandler
{
    Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default);
}
