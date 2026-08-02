using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Identity;
using PersonalLibrary.Application.Identity.Commands;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IRegisterUserHandler registerUser) : ControllerBase
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
            new RegisterUserCommand(request.Email, request.Password, request.Name),
            cancellationToken);

        return Created($"/api/users/{result.UserId}", result);
    }
}