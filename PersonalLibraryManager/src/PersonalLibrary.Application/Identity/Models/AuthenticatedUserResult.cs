namespace PersonalLibrary.Application.Identity.Models;

public sealed record AuthenticatedUserResult(
    Guid? UserId,
    string? Email,
    string? Name,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<IdentityError> Errors)
{
    public bool Succeeded => UserId.HasValue && Errors.Count == 0;
}
