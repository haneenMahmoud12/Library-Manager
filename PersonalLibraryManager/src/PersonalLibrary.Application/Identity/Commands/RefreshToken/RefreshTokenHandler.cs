using PersonalLibrary.Application.Identity.Exceptions;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.Application.Identity.Commands.RefreshToken;

public sealed class RefreshTokenHandler(ITokenService tokenService)
    : IRefreshTokenHandler
{
    public async Task<TokenPairResult> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            throw new AuthenticationFlowException("InvalidRefreshToken", "Invalid refresh token.");

        return await tokenService.RefreshAsync(command.RefreshToken, cancellationToken)
            ?? throw new AuthenticationFlowException("InvalidRefreshToken", "Invalid or expired refresh token.");
    }
}
