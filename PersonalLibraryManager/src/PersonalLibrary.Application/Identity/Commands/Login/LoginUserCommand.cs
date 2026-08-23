namespace PersonalLibrary.Application.Identity.Commands.Login;

public sealed record LoginUserCommand(string Email, string Password);