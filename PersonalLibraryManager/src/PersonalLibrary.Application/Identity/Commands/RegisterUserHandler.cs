using System.ComponentModel.DataAnnotations;
using PersonalLibrary.Application.Identity.Exceptions;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.Application.Identity.Commands;

public sealed class RegisterUserHandler(IIdentityService identityService)
    : IRegisterUserHandler
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public async Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email = command.Email?.Trim();
        var name = command.Name?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > 320 ||
            !EmailValidator.IsValid(email))
        {
            throw new RegistrationValidationException(
                [new("InvalidEmail", "A valid email address is required.")]);
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
        {
            throw new RegistrationValidationException(
                [new("InvalidName", "Name is required and cannot exceed 150 characters.")]);
        }

        if (string.IsNullOrEmpty(command.Password) || command.Password.Length > 128)
        {
            throw new RegistrationValidationException(
                [new("InvalidPassword", "Password is required and cannot exceed 128 characters.")]);
        }

        var creation = await identityService.CreateUserAsync(
            email,
            command.Password,
            name,
            cancellationToken);

        if (!creation.Succeeded)
            throw new RegistrationValidationException(creation.Errors);

        return new RegisterUserResult(
            creation.UserId!.Value,
            creation.Email!,
            creation.Name!);
    }
}
