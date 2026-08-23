using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Application.Identity.Exceptions;

namespace PersonalLibrary.Api.ExceptionHandling;

internal sealed class AuthenticationFlowExceptionHandler(
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not AuthenticationFlowException authenticationException)
            return false;

        var status = authenticationException.Code switch
        {
            "EmailNotConfirmed" => StatusCodes.Status403Forbidden,
            "LockedOut" => StatusCodes.Status429TooManyRequests,
            "InvalidConfirmation" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status401Unauthorized
        };

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = authenticationException.Message
            },
            Exception = exception
        });
    }
}
