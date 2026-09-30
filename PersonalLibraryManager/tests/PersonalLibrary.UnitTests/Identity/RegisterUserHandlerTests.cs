using PersonalLibrary.Application.Identity;
using PersonalLibrary.Application.Identity.Exceptions;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.UnitTests.Identity;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task HandleAsync_normalizes_input_creates_user_and_sends_confirmation()
    {
        var userId = Guid.NewGuid();
        var identity = new StubIdentityService(
            new(userId, "reader@example.com", "Reader", []));
        var email = new StubEmailService();
        var service = new AuthService(identity, new StubTokenService(), email);

        var result = await service.RegisterAsync(
            new RegisterDto("  reader@example.com  ", "Password1", "Password1", "  Reader  "));

        Assert.Equal(userId, result.UserId);
        Assert.Equal("reader@example.com", identity.Email);
        Assert.Equal("Reader", identity.Name);
        Assert.True(email.WasCalled);
    }

    [Fact]
    public async Task HandleAsync_rejects_invalid_email_before_calling_identity()
    {
        var identity = new StubIdentityService(
            new(Guid.NewGuid(), "unused@example.com", "Unused", []));
        var service = new AuthService(identity, new StubTokenService(), new StubEmailService());

        await Assert.ThrowsAsync<RegistrationValidationException>(() =>
            service.RegisterAsync(new("not-an-email", "Password1", "Password1", "Reader")));

        Assert.False(identity.WasCalled);
    }

    [Fact]
    public async Task HandleAsync_propagates_identity_validation_as_application_error()
    {
        var identity = new StubIdentityService(
            new(null, null, null, [new("DuplicateEmail", "Already registered.")]));
        var service = new AuthService(identity, new StubTokenService(), new StubEmailService());

        var exception = await Assert.ThrowsAsync<RegistrationValidationException>(() =>
            service.RegisterAsync(new("reader@example.com", "Password1", "Password1", "Reader")));

        Assert.Contains(exception.Errors, error => error.Code == "DuplicateEmail");
    }

    [Fact]
    public async Task HandleAsync_rejects_password_mismatch_before_calling_identity()
    {
        var identity = new StubIdentityService(
            new(Guid.NewGuid(), "reader@example.com", "Reader", []));
        var service = new AuthService(identity, new StubTokenService(), new StubEmailService());

        await Assert.ThrowsAsync<RegistrationValidationException>(() =>
            service.RegisterAsync(new("reader@example.com", "Password1", "Password2", "Reader")));

        Assert.False(identity.WasCalled);
    }

    private sealed class StubIdentityService(CreateIdentityUserResult result)
        : IIdentityService
    {
        public bool WasCalled { get; private set; }
        public string? Email { get; private set; }
        public string? Name { get; private set; }

        public Task<CreateIdentityUserResult> CreateUserAsync(
            string email,
            string password,
            string name,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            Email = email;
            Name = name;
            return Task.FromResult(result);
        }

        public Task<AuthenticatedUserResult> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<EmailConfirmationChallenge?> CreateEmailConfirmationChallengeAsync(
            string email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EmailConfirmationChallenge?>(
                new(result.UserId!.Value, result.Email!, result.Name!, "token"));

        public Task<IdentityOperationResult> ConfirmEmailAsync(
            Guid userId,
            string token,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubEmailService : IEmailService
    {
        public bool WasCalled { get; private set; }

        public Task SendConfirmationEmailAsync(
            EmailConfirmationChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubTokenService : ITokenService
    {
        public Task<TokenPairResult> IssueAsync(
            AuthenticatedUserResult user,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TokenPairResult?> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
