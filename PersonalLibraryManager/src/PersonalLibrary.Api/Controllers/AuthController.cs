using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    IRegisterUserHandler registerUser,
    ILoginHandler loginUser,
    IConfirmEmailHandler confirmEmail,
    IRefreshTokenHandler refreshToken,
    IResendConfirmationHandler resendConfirmation)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<RegisterUserResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterUserResult>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registerUser.HandleAsync(
            new RegisterUserCommand(request.Email, request.Password, request.ConfirmPassword, request.Name),
            cancellationToken);

        return Created($"/api/users/{result.UserId}", result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginUserResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginUserResult>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginUser.HandleAsync(
            new LoginUserCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] Guid userId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        await confirmEmail.HandleAsync(
            new ConfirmEmailCommand(userId, token),
            cancellationToken);

        return Ok(new { message = "Email confirmed. You can now sign in." });
    }

    [AllowAnonymous]
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ResendConfirmation(
        ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        await resendConfirmation.HandleAsync(
            new ResendConfirmationCommand(request.Email),
            cancellationToken);

        return Accepted(new
        {
            message = "If the account exists and is unconfirmed, a confirmation email has been sent."
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<TokenPairResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenPairResult>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await refreshToken.HandleAsync(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        return Ok(result);
    }
}