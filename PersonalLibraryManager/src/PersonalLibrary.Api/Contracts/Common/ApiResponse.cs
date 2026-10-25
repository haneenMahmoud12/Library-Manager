namespace PersonalLibrary.Api.Contracts.Common;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message,
    ApiError? Error,
    IReadOnlyDictionary<string, ApiLink>? Links = null);

public sealed record ApiLink(string Href, string Method);

public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Details = null,
    string? TraceId = null);

public static class ApiResponse
{
    public static ApiResponse<T> Succeeded<T>(
        T data,
        string? message = null,
        IReadOnlyDictionary<string, ApiLink>? links = null) =>
        new(true, data, message, null, links);

    public static ApiResponse<object?> Succeeded(
        string? message = null,
        IReadOnlyDictionary<string, ApiLink>? links = null) =>
        new(true, null, message, null, links);

    public static ApiResponse<object?> Failed(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? details = null,
        string? traceId = null) =>
        new(false, null, null, new ApiError(code, message, details, traceId), null);
}

public static class ApiLinks
{
    public static IReadOnlyDictionary<string, ApiLink> Self(string href) =>
        new Dictionary<string, ApiLink>
        {
            ["self"] = new(href, HttpMethods.Get)
        };

    public static IReadOnlyDictionary<string, ApiLink> ForResource(
        string self,
        params (string Relation, string Href, string Method)[] related)
    {
        var links = new Dictionary<string, ApiLink>
        {
            ["self"] = new(self, HttpMethods.Get)
        };

        foreach (var (relation, href, method) in related)
            links[relation] = new ApiLink(href, method);

        return links;
    }

    public static IReadOnlyDictionary<string, ApiLink> Actions(
        params (string Relation, string Href, string Method)[] actions) =>
        actions.ToDictionary(
            action => action.Relation,
            action => new ApiLink(action.Href, action.Method));
}
