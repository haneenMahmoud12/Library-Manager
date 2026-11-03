using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Identity;
using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.IntegrationTests.Identity;

public sealed class IdentityAcceptanceTests(RegistrationApiFactory factory)
    : IClassFixture<RegistrationApiFactory>
{
    [Fact]
    public async Task Confirmation_login_refresh_and_logout_complete_the_identity_session_lifecycle()
    {
        var email = $"identity-{Guid.NewGuid():N}@example.com";
        using var client = factory.CreateClient();
        var registration = await RegisterAsync(client, email);
        var challenge = factory.Email.Messages.Single(message => message.UserId == registration.UserId);

        var blockedLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password1"
        });
        Assert.Equal(HttpStatusCode.Forbidden, blockedLogin.StatusCode);

        var confirmation = await client.PostAsJsonAsync("/api/auth/email-confirmations", new ConfirmEmailRequest
        {
            UserId = registration.UserId,
            Token = challenge.Token
        });
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password1"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginPayload = await login.Content.ReadFromJsonAsync<ApiResponse<LoginViewModel>>();
        Assert.NotNull(loginPayload?.Data);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.Data.Tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.Data.Tokens.RefreshToken));

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = loginPayload.Data.Tokens.RefreshToken
        });
        Assert.True(
            refresh.StatusCode == HttpStatusCode.OK,
            $"Expected refresh success but received {(int)refresh.StatusCode}: {await refresh.Content.ReadAsStringAsync()}");
        var refreshed = await refresh.Content.ReadFromJsonAsync<ApiResponse<TokenPairResult>>();
        Assert.NotNull(refreshed?.Data);
        Assert.NotEqual(loginPayload.Data.Tokens.RefreshToken, refreshed.Data.RefreshToken);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshed.Data.AccessToken);
        var logout = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest
        {
            RefreshToken = refreshed.Data.RefreshToken
        });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = refreshed.Data.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Confirmation_resend_and_identity_failures_use_repeatable_problem_responses()
    {
        var email = $"resend-{Guid.NewGuid():N}@example.com";
        using var client = factory.CreateClient();
        var registration = await RegisterAsync(client, email);
        var initialCount = factory.Email.Messages.Count(message => message.UserId == registration.UserId);

        var resend = await client.PostAsJsonAsync(
            "/api/auth/resend-confirmation",
            new ResendConfirmationRequest { Email = email });
        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Equal(initialCount + 1, factory.Email.Messages.Count(message => message.UserId == registration.UserId));

        var invalidConfirmation = await client.PostAsJsonAsync(
            "/api/auth/email-confirmations",
            new ConfirmEmailRequest { UserId = registration.UserId, Token = "invalid-token" });
        await AssertProblemAsync(invalidConfirmation, HttpStatusCode.BadRequest);

        var invalidLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "WrongPassword1"
        });
        await AssertProblemAsync(invalidLogin, HttpStatusCode.Unauthorized);

        var invalidRefresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = "not-a-refresh-token" });
        await AssertProblemAsync(invalidRefresh, HttpStatusCode.Unauthorized);

        var anonymousLogout = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new LogoutRequest { RefreshToken = "not-a-refresh-token" });
        await AssertProblemAsync(anonymousLogout, HttpStatusCode.Unauthorized);
    }

    internal static async Task<(HttpClient Client, LoginViewModel Login)> CreateAuthenticatedClientAsync(
        RegistrationApiFactory factory)
    {
        var email = $"api-{Guid.NewGuid():N}@example.com";
        var client = factory.CreateClient();
        var registration = await RegisterAsync(client, email);
        var challenge = factory.Email.Messages.Single(message => message.UserId == registration.UserId);
        var confirmation = await client.PostAsJsonAsync("/api/auth/email-confirmations", new ConfirmEmailRequest
        {
            UserId = registration.UserId,
            Token = challenge.Token
        });
        confirmation.EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password1"
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginViewModel>>();
        Assert.NotNull(payload?.Data);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload.Data.Tokens.AccessToken);
        return (client, payload.Data);
    }

    private static async Task<RegisterViewModel> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password1",
            ConfirmPassword = "Password1",
            Name = "Acceptance Reader"
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterViewModel>>();
        Assert.NotNull(payload?.Data);
        return payload.Data;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.True(
            response.StatusCode == status,
            $"Expected {(int)status} but received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("code").GetString()));
    }
}
