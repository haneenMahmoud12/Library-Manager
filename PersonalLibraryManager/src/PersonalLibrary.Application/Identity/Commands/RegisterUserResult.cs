namespace PersonalLibrary.Application.Identity.Commands;

public sealed record RegisterUserResult(
    Guid UserId,
    string Email,
    string Name);