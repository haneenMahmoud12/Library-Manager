using System.ComponentModel.DataAnnotations;

namespace PersonalLibrary.Api.Contracts.Identity;

public sealed record LogoutRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}
