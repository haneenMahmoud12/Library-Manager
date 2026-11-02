using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Identity;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Infrastructure.Persistence;

namespace PersonalLibrary.IntegrationTests.Identity;

public sealed class RegistrationAcceptanceTests(RegistrationApiFactory factory)
    : IClassFixture<RegistrationApiFactory>
{
    [Fact]
    public async Task Valid_details_create_a_persisted_user_and_send_confirmation_email()
    {
        var email = UniqueEmail();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            ValidRequest(email, "  Acceptance Reader  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterViewModel>>();
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.NotNull(payload.Data);
        Assert.Equal(email, payload.Data.Email);
        Assert.Equal("Acceptance Reader", payload.Data.Name);
        Assert.Equal("/api/auth/email-confirmations", payload.Links!["confirmEmail"].Href);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.SingleAsync(candidate =>
            candidate.NormalizedEmail == email.ToUpperInvariant());
        Assert.Equal(payload.Data.UserId, user.Id);
        Assert.Equal("Acceptance Reader", user.DisplayName);
        Assert.False(user.EmailConfirmed);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));

        Assert.Contains(factory.Email.Messages, message =>
            message.UserId == user.Id &&
            message.Email == email &&
            !string.IsNullOrWhiteSpace(message.Token));
    }

    [Fact]
    public async Task Registration_is_anonymous_and_does_not_require_an_access_token()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            ValidRequest(UniqueEmail()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Invalid_details_return_repeatable_validation_responses(RegisterRequest request)
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.GetProperty("code").GetString() is
            "ValidationFailed" or "RegistrationValidationFailed");
        Assert.True(problem.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.EnumerateObject().Any());
    }

    [Fact]
    public async Task Registering_the_same_email_twice_returns_conflict_and_keeps_one_account()
    {
        var email = UniqueEmail();
        using var client = factory.CreateClient();
        var first = await client.PostAsJsonAsync("/api/auth/register", ValidRequest(email));

        var duplicate = await client.PostAsJsonAsync(
            "/api/auth/register",
            ValidRequest(email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        Assert.Equal("DuplicateEmail", problem.RootElement.GetProperty("code").GetString());

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await context.Users.CountAsync(user => user.NormalizedEmail == email.ToUpperInvariant()));
    }

    public static TheoryData<RegisterRequest> InvalidRequests => new()
    {
        new RegisterRequest
        {
            Email = "not-an-email",
            Password = "Password1",
            ConfirmPassword = "Password1",
            Name = "Reader"
        },
        new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password1",
            ConfirmPassword = "Different1",
            Name = "Reader"
        },
        new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "password",
            ConfirmPassword = "password",
            Name = "Reader"
        },
        new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password1",
            ConfirmPassword = "Password1",
            Name = ""
        }
    };

    private static RegisterRequest ValidRequest(string email, string name = "Acceptance Reader") =>
        new()
        {
            Email = email,
            Password = "Password1",
            ConfirmPassword = "Password1",
            Name = name
        };

    private static string UniqueEmail() => $"reader-{Guid.NewGuid():N}@example.com";
}
