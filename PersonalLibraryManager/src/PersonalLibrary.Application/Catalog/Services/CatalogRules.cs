using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Common.Authentication;

namespace PersonalLibrary.Application.Catalog.Services;

internal static class CatalogRules
{
    public static void RequireAuthenticatedUser(ICurrentUserContext currentUser)
    {
        if (currentUser.UserId is not { } userId || userId == Guid.Empty)
            throw new UnauthorizedAccessException("An authenticated user is required.");
    }

    public static void ValidateId(Guid id, string field)
    {
        if (id == Guid.Empty)
            throw new CatalogValidationException(field, $"A valid {field} is required.");
    }

    public static string RequiredText(string? value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
            throw new CatalogValidationException(field, $"{field} is required and cannot exceed {maxLength} characters.");
        return normalized;
    }

    public static string? OptionalText(string? value, int? maxLength, string field)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (maxLength.HasValue && normalized?.Length > maxLength)
            throw new CatalogValidationException(field, $"{field} cannot exceed {maxLength} characters.");
        return normalized;
    }

    public static string? OptionalUrl(string? value, string field)
    {
        var normalized = OptionalText(value, 1000, field);
        if (normalized is not null &&
            (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
             uri.Scheme is not ("http" or "https")))
        {
            throw new CatalogValidationException(field, $"{field} must be an absolute HTTP or HTTPS URL.");
        }
        return normalized;
    }
}
