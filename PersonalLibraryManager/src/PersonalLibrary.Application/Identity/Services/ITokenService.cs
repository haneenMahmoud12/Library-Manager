using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Services;

public interface ITokenService
{
    Task<TokenPairResult> IssueAsync(
        AuthenticatedUserResult user,
        CancellationToken cancellationToken = default);

    Task<TokenPairResult?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
