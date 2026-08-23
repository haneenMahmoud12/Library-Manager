using System.ComponentModel.DataAnnotations;

namespace PersonalLibrary.Api.Contracts.Identity;

public sealed record RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}
