namespace PersonalLibrary.Application.Identity.Models;

public sealed record TokenPairResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    string TokenType = "Bearer");
