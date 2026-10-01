using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Application.Identity.Exceptions;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Catalog.Exceptions;

namespace PersonalLibrary.Api.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {TraceId} was cancelled by the client.", context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
                throw;

            await WriteExceptionAsync(context, exception);
        }
    }

    private async Task WriteExceptionAsync(HttpContext context, Exception exception)
    {
        var mapped = MapException(exception);

        if (mapped.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for request {Method} {Path}. Trace ID: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request failed with status {StatusCode}. Trace ID: {TraceId}",
                mapped.StatusCode,
                context.TraceIdentifier);
        }

        context.Response.Clear();
        context.Response.StatusCode = mapped.StatusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            ApiResponse.Failed(
                mapped.Code,
                mapped.Message,
                mapped.Details,
                context.TraceIdentifier),
            context.RequestAborted);
    }

    private static ExceptionResponse MapException(Exception exception) => exception switch
    {
        RegistrationValidationException registration => MapRegistrationException(registration),
        AuthenticationFlowException authentication => MapAuthenticationException(authentication),
        CatalogValidationException catalog => new(
            StatusCodes.Status400BadRequest,
            "CatalogValidationFailed",
            "Catalog validation failed.",
            new Dictionary<string, string[]>
            {
                [catalog.Field] = [catalog.ValidationMessage]
            }),
        BookNotFoundException => new(StatusCodes.Status404NotFound, "BookNotFound", "The requested book was not found."),
        AuthorNotFoundException => new(StatusCodes.Status404NotFound, "AuthorNotFound", "The requested author was not found."),
        PublisherNotFoundException => new(StatusCodes.Status404NotFound, "PublisherNotFound", "The requested publisher was not found."),
        BookEditionNotFoundException => new(StatusCodes.Status404NotFound, "BookEditionNotFound", "The requested book edition was not found."),
        BookMetadataNotFoundException => new(
            StatusCodes.Status404NotFound,
            "BookMetadataNotFound",
            "No book metadata was found for the supplied ISBN."),
        BookMetadataProviderException => new(
            StatusCodes.Status503ServiceUnavailable,
            "BookMetadataProvidersUnavailable",
            "Book metadata providers are temporarily unavailable."),
        LibraryValidationException library => new(
            StatusCodes.Status400BadRequest,
            "LibraryValidationFailed",
            "Library validation failed.",
            new Dictionary<string, string[]>
            {
                [library.Field] = [library.ValidationMessage]
            }),
        LibraryNotFoundException => new(
            StatusCodes.Status404NotFound,
            "LibraryNotFound",
            "The requested library was not found."),
        LibraryAccessDeniedException => new(
            StatusCodes.Status403Forbidden,
            "LibraryAccessDenied",
            "You do not have permission to modify this library."),
        UnauthorizedAccessException => new(
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "Authentication is required."),
        KeyNotFoundException => new(
            StatusCodes.Status404NotFound,
            "NotFound",
            "The requested resource was not found."),
        BadHttpRequestException badRequest => new(
            badRequest.StatusCode,
            "BadRequest",
            "The request could not be processed."),
        ArgumentException argument => new(
            StatusCodes.Status400BadRequest,
            "InvalidArgument",
            "One or more request arguments are invalid.",
            new Dictionary<string, string[]>
            {
                [argument.ParamName ?? "Request"] = [argument.Message]
            }),
        _ => new(
            StatusCodes.Status500InternalServerError,
            "InternalServerError",
            "An unexpected error occurred.")
    };

    private static ExceptionResponse MapRegistrationException(
        RegistrationValidationException exception)
    {
        var details = exception.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());
        var duplicateEmail = exception.Errors.Any(error =>
            error.Code is "DuplicateEmail" or "DuplicateUserName");

        return duplicateEmail
            ? new(
                StatusCodes.Status409Conflict,
                "DuplicateEmail",
                "The email address is already registered.",
                details)
            : new(
                StatusCodes.Status400BadRequest,
                "RegistrationValidationFailed",
                "Registration validation failed.",
                details);
    }

    private static ExceptionResponse MapAuthenticationException(
        AuthenticationFlowException exception)
    {
        var statusCode = exception.Code switch
        {
            "EmailNotConfirmed" => StatusCodes.Status403Forbidden,
            "LockedOut" => StatusCodes.Status429TooManyRequests,
            "InvalidConfirmation" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status401Unauthorized
        };

        return new(statusCode, exception.Code, exception.Message);
    }

    private sealed record ExceptionResponse(
        int StatusCode,
        string Code,
        string Message,
        IReadOnlyDictionary<string, string[]>? Details = null);
}
