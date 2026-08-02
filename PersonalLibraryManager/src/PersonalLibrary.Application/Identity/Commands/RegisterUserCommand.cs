namespace PersonalLibrary.Application.Identity.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string Name);