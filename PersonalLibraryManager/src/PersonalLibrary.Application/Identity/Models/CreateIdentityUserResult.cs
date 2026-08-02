namespace PersonalLibrary.Application.Identity.Models;

public sealed record CreateIdentityUserResult(
    Guid? UserId,
    string? Email,
    string? Name,
    IReadOnlyCollection<IdentityError> Errors)
{
    public bool Succeeded => UserId.HasValue && Errors.Count == 0;
}
