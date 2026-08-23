namespace PersonalLibrary.Application.Identity.Models;

public sealed record EmailConfirmationChallenge(
    Guid UserId,
    string Email,
    string Name,
    string Token);
