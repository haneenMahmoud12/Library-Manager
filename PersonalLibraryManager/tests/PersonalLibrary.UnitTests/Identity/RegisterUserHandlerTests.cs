using PersonalLibrary.Application.Identity.Commands;
using PersonalLibrary.Application.Identity.Exceptions;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.UnitTests.Identity;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task HandleAsync_normalizes_input_and_returns_created_user()
    {
        var userId = Guid.NewGuid();
        var identity = new StubIdentityService(
            new(userId, "reader@example.com", "Reader", []));
        var handler = new RegisterUserHandler(identity);

        var result = await handler.HandleAsync(
            new("  reader@example.com  ", "Password1", "  Reader  "));

        Assert.Equal(userId, result.UserId);
        Assert.Equal("reader@example.com", identity.Email);
        Assert.Equal("Reader", identity.Name);
    }

    [Fact]
    public async Task HandleAsync_rejects_invalid_email_before_calling_identity()
    {
        var identity = new StubIdentityService(
            new(Guid.NewGuid(), "unused@example.com", "Unused", []));
        var handler = new RegisterUserHandler(identity);

        await Assert.ThrowsAsync<RegistrationValidationException>(() =>
            handler.HandleAsync(new("not-an-email", "Password1", "Reader")));

        Assert.False(identity.WasCalled);
    }

    [Fact]
    public async Task HandleAsync_propagates_identity_validation_as_application_error()
    {
        var identity = new StubIdentityService(
            new(null, null, null, [new("DuplicateEmail", "Already registered.")]));
        var handler = new RegisterUserHandler(identity);

        var exception = await Assert.ThrowsAsync<RegistrationValidationException>(() =>
            handler.HandleAsync(new("reader@example.com", "Password1", "Reader")));

        Assert.Contains(exception.Errors, error => error.Code == "DuplicateEmail");
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
    }
}
