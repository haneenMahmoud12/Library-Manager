namespace PersonalLibrary.Application.Identity.Models;

public sealed record IdentityOperationResult(
    bool Succeeded,
    IReadOnlyCollection<IdentityError> Errors);
