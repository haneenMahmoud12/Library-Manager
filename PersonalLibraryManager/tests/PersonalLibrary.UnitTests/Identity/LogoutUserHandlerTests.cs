using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.UnitTests.Identity;

public sealed class LogoutUserHandlerTests
{
    [Fact]
    public async Task LogoutAsync_revokes_the_authenticated_users_refresh_session()
    {
        var userId = Guid.NewGuid();
        var tokens = new StubTokenService();
        var service = new AuthService(
            new StubIdentityService(),
            tokens,
            new StubEmailService());

        await service.LogoutAsync(userId, "refresh-token");

        Assert.Equal(userId, tokens.RevokedUserId);
        Assert.Equal("refresh-token", tokens.RevokedToken);
    }

    [Fact]
    public async Task LogoutAsync_rejects_a_missing_authenticated_user()
    {
        var tokens = new StubTokenService();
        var service = new AuthService(
            new StubIdentityService(),
            tokens,
            new StubEmailService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LogoutAsync(Guid.Empty, "refresh-token"));

        Assert.Null(tokens.RevokedToken);
    }

    private sealed class StubTokenService : ITokenService
    {
        public Guid? RevokedUserId { get; private set; }
        public string? RevokedToken { get; private set; }

        public Task RevokeAsync(
            Guid userId,
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            RevokedUserId = userId;
            RevokedToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task<TokenPairResult> IssueAsync(
            AuthenticatedUserResult user,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TokenPairResult?> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubIdentityService : IIdentityService
    {
        public Task<AuthenticatedUserResult> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CreateIdentityUserResult> CreateUserAsync(
            string email,
            string password,
            string name,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<EmailConfirmationChallenge?> CreateEmailConfirmationChallengeAsync(
            string email,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IdentityOperationResult> ConfirmEmailAsync(
            Guid userId,
            string token,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubEmailService : IEmailService
    {
        public Task SendConfirmationEmailAsync(
            EmailConfirmationChallenge challenge,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
