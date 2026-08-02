using System.ComponentModel.DataAnnotations;

namespace PersonalLibrary.Api.Contracts.Identity;

public sealed record RegisterRequest
{
    [Required, EmailAddress, StringLength(320)]
    public required string Email { get; init; }

    [Required, StringLength(128, MinimumLength = 1)]
    public required string Password { get; init; }

    [Required]
    [Compare(
        nameof(Password),
        ErrorMessage = "Password and confirmation password must match.")]
    public required string ConfirmPassword { get; init; }

    [Required, StringLength(150)]
    public required string Name { get; init; }
}
