using PersonalLibrary.Application.Identity;
using PersonalLibrary.Application.Identity.Exceptions;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.UnitTests.Identity;

public sealed class LoginUserHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_user_and_token_pair_after_authentication()
    {
        var userId = Guid.NewGuid();
        var identity = new StubIdentityService(
            new(userId, "reader@example.com", "Reader", [], []));
        var expectedTokens = new TokenPairResult(
            "access",
            DateTime.UtcNow.AddMinutes(10),
            "refresh",
            DateTime.UtcNow.AddDays(30));
        var service = new AuthService(identity, new StubTokenService(expectedTokens), new StubEmailService());

        var result = await service.LoginAsync(
            new LoginDto(" reader@example.com ", "Password1"));

        Assert.Equal(userId, result.UserId);
        Assert.Equal("access", result.Tokens.AccessToken);
        Assert.Equal("refresh", result.Tokens.RefreshToken);
    }

    [Fact]
    public async Task HandleAsync_does_not_issue_tokens_for_unconfirmed_email()
    {
        var identity = new StubIdentityService(
            new(null, null, null, [], [new("EmailNotConfirmed", "Confirm email.")]));
        var tokens = new StubTokenService(null);
        var service = new AuthService(identity, tokens, new StubEmailService());

        var exception = await Assert.ThrowsAsync<AuthenticationFlowException>(() =>
            service.LoginAsync(new("reader@example.com", "Password1")));

        Assert.Equal("EmailNotConfirmed", exception.Code);
        Assert.False(tokens.WasCalled);
    }

    private sealed class StubIdentityService(AuthenticatedUserResult result)
        : IIdentityService
    {
        public Task<AuthenticatedUserResult> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(result);

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

    private sealed class StubTokenService(TokenPairResult? result) : ITokenService
    {
        public bool WasCalled { get; private set; }

        public Task<TokenPairResult> IssueAsync(
            AuthenticatedUserResult user,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(result!);
        }

        public Task<TokenPairResult?> RefreshAsync(
            string refreshToken,
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
