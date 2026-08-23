namespace PersonalLibrary.Application.Identity.Commands.Login;

public interface ILoginHandler
{
    Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default);
}