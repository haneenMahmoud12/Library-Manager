namespace PersonalLibrary.Application.Identity.Models;

public sealed record LoginViewModel(
    Guid UserId,
    string Email,
    string Name,
    TokenPairResult Tokens);
