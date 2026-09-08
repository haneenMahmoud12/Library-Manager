using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Identity;
using PersonalLibrary.Application.Identity.Commands.ConfirmEmail;
using PersonalLibrary.Application.Identity.Commands.Login;
using PersonalLibrary.Application.Identity.Commands.RefreshToken;
using PersonalLibrary.Application.Identity.Commands.Register;
using PersonalLibrary.Application.Identity.Commands.ResendConfirmation;
using PersonalLibrary.Application.Identity.Models;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    RegisterUserHandler registerUser,
    LoginUserHandler loginUser,
    ConfirmEmailHandler confirmEmail,
    RefreshTokenHandler refreshToken,
    ResendConfirmationHandler resendConfirmation)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<ApiResponse<RegisterUserResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RegisterUserResult>>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registerUser.HandleAsync(
            new RegisterUserCommand(
                request.Email,
                request.Password,
                request.ConfirmPassword,
                request.Name),
            cancellationToken);

        return Created(
            $"/api/users/{result.UserId}",
            ApiResponse.Succeeded(result, "Registration succeeded. Please confirm your email."));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<ApiResponse<LoginUserResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LoginUserResult>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginUser.HandleAsync(
            new LoginUserCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(ApiResponse.Succeeded(result, "Login succeeded."));
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object?>>> ConfirmEmail(
        [FromQuery] Guid userId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        await confirmEmail.HandleAsync(
            new ConfirmEmailCommand(userId, token),
            cancellationToken);

        return Ok(ApiResponse.Succeeded("Email confirmed. You can now sign in."));
    }

    [AllowAnonymous]
    [HttpPost("resend-confirmation")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<ApiResponse<object?>>> ResendConfirmation(
        ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        await resendConfirmation.HandleAsync(
            new ResendConfirmationCommand(request.Email),
            cancellationToken);

        return Accepted(ApiResponse.Succeeded(
            "If the account exists and is unconfirmed, a confirmation email has been sent."));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<ApiResponse<TokenPairResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TokenPairResult>>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await refreshToken.HandleAsync(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        return Ok(ApiResponse.Succeeded(result, "Token refreshed."));
    }
}
