using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Application.Identity.Exceptions;

namespace PersonalLibrary.Api.ExceptionHandling;

internal sealed class RegistrationValidationExceptionHandler(
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RegistrationValidationException registrationException)
            return false;

        var duplicateEmail = registrationException.Errors.Any(error =>
            error.Code is "DuplicateEmail" or "DuplicateUserName");

        if (duplicateEmail)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Email already registered"
                },
                Exception = exception
            });
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        var errors = registrationException.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Registration validation failed"
            },
            Exception = exception
        });
    }
}
