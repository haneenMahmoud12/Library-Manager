namespace PersonalLibrary.Api.Contracts.Common;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message,
    ApiError? Error);

public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Details = null,
    string? TraceId = null);

public static class ApiResponse
{
    public static ApiResponse<T> Succeeded<T>(
        T data,
        string? message = null) =>
        new(true, data, message, null);

    public static ApiResponse<object?> Succeeded(
        string? message = null) =>
        new(true, null, message, null);

    public static ApiResponse<object?> Failed(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? details = null,
        string? traceId = null) =>
        new(false, null, null, new ApiError(code, message, details, traceId));
}
