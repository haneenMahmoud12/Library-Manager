using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Identity;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    ICurrentUserContext currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<ApiResponse<RegisterViewModel>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RegisterViewModel>>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(
            new RegisterDto(
                request.Email,
                request.Password,
                request.ConfirmPassword,
                request.Name),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Succeeded(
                result,
                "Registration succeeded. Please confirm your email.",
                ApiLinks.Actions(
                    ("confirmEmail", "/api/auth/email-confirmations", HttpMethods.Post),
                    ("login", "/api/auth/login", HttpMethods.Post))));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<ApiResponse<LoginViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LoginViewModel>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            new LoginDto(request.Email, request.Password),
            cancellationToken);

        return Ok(ApiResponse.Succeeded(result, "Login succeeded."));
    }

    [AllowAnonymous]
    [HttpGet("email-confirmations/prompt")]
    [Produces("text/html")]
    public ContentResult EmailConfirmationPrompt(
        [FromQuery] Guid userId,
        [FromQuery] string token)
    {
        var encodedUserId = HtmlEncoder.Default.Encode(userId.ToString("D"));
        var encodedToken = HtmlEncoder.Default.Encode(token);
        const string action = "/api/auth/email-confirmations";
        var html = $$"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><title>Confirm email</title></head>
            <body>
              <main>
                <h1>Confirm your email address</h1>
                <form method="post" action="{{action}}">
                  <input type="hidden" name="UserId" value="{{encodedUserId}}">
                  <input type="hidden" name="Token" value="{{encodedToken}}">
                  <button type="submit">Confirm email</button>
                </form>
              </main>
            </body>
            </html>
            """;
        return Content(html, "text/html");
    }

    [AllowAnonymous]
    [HttpPost("email-confirmations")]
    [Consumes("application/json")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object?>>> ConfirmEmail(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ConfirmEmailAsync(request.UserId, request.Token, cancellationToken);

        return Ok(ApiResponse.Succeeded(message: "Email confirmed. You can now sign in."));
    }

    [AllowAnonymous]
    [HttpPost("email-confirmations")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("text/html")]
    public async Task<ContentResult> ConfirmEmailFromBrowser(
        [FromForm] ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ConfirmEmailAsync(request.UserId, request.Token, cancellationToken);
        return Content(
            "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>Email confirmed</title></head><body><main><h1>Email confirmed</h1><p>You can now sign in.</p></main></body></html>",
            "text/html");
    }

    [AllowAnonymous]
    [HttpPost("resend-confirmation")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<ApiResponse<object?>>> ResendConfirmation(
        ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ResendConfirmationAsync(request.Email, cancellationToken);

        return Accepted(ApiResponse.Succeeded(
            message: "If the account exists and is unconfirmed, a confirmation email has been sent."));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<ApiResponse<TokenPairResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TokenPairResult>>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        return Ok(ApiResponse.Succeeded(result, "Token refreshed."));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(
            currentUser.UserId ?? Guid.Empty,
            request.RefreshToken,
            cancellationToken);

        return NoContent();
    }
}
