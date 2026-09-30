namespace PersonalLibrary.Application.Identity.Models;

public sealed record RegisterDto(
    string Email,
    string Password,
    string ConfirmPassword,
    string Name);
