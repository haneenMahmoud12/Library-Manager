using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Application.Identity.Services;

public interface IAuthService
{
    Task<RegisterViewModel> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default);

    Task<LoginViewModel> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default);

    Task ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default);

    Task ResendConfirmationAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<TokenPairResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
