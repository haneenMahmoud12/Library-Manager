using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Commands.RefreshToken;

public interface IRefreshTokenHandler
{
    Task<TokenPairResult> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default);
}
