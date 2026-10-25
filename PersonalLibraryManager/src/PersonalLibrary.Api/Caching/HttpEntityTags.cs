namespace PersonalLibrary.Api.Caching;

public static class HttpEntityTags
{
    public static bool IsNotModified(
        HttpContext context,
        Guid id,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        var version = (updatedAt ?? createdAt).ToUniversalTime();
        var entityTag = $"\"{id:N}-{version.Ticks:x}\"";

        context.Response.Headers.ETag = entityTag;
        context.Response.Headers.LastModified = version.ToString("R");

        var supplied = context.Request.Headers.IfNoneMatch.ToString();
        if (string.IsNullOrWhiteSpace(supplied))
            return false;

        return supplied
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(candidate => candidate == "*" ||
                              string.Equals(candidate, entityTag, StringComparison.Ordinal));
    }
}
