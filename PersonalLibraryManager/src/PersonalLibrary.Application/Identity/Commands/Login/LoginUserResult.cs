using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Commands.Login;

public sealed record LoginUserResult(
    Guid UserId,
    string Email,
    string Name,
    TokenPairResult Tokens);