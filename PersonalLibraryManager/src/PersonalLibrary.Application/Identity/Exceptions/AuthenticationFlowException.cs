using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Exceptions;

public sealed class AuthenticationFlowException(
    string code,
    string description,
    IReadOnlyCollection<IdentityError>? errors = null)
    : Exception(description)
{
    public string Code { get; } = code;
    public IReadOnlyCollection<IdentityError> Errors { get; } =
        errors ?? [new(code, description)];
}
