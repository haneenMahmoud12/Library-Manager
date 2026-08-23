namespace PersonalLibrary.Application.Identity.Commands.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string Name);