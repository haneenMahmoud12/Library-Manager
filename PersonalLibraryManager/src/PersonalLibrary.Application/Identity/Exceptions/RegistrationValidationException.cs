using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Exceptions;

public sealed class RegistrationValidationException(
    IReadOnlyCollection<IdentityError> errors)
    : Exception("User registration failed validation.")
{
    public IReadOnlyCollection<IdentityError> Errors { get; } = errors;
}
