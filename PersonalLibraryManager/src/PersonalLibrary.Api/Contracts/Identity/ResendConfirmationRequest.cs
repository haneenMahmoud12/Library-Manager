using System.ComponentModel.DataAnnotations;

namespace PersonalLibrary.Api.Contracts.Identity;

public sealed record ResendConfirmationRequest
{
    [Required, EmailAddress, StringLength(320)]
    public required string Email { get; init; }
}
