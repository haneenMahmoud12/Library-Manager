namespace PersonalLibrary.Application.Identity.Commands.Register;

public sealed record RegisterUserResult(
    Guid UserId,
    string Email,
    string Name);