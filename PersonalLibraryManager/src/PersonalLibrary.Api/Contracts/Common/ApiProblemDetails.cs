using Microsoft.AspNetCore.Mvc;

namespace PersonalLibrary.Api.Contracts.Common;

public static class ApiProblemDetails
{
    public static ProblemDetails Create(
        int status,
        string code,
        string title,
        string? detail = null,
        IReadOnlyDictionary<string, string[]>? errors = null,
        IReadOnlyDictionary<string, ApiLink>? links = null,
        string? traceId = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{status}"
        };

        problem.Extensions["code"] = code;
        if (errors is not null)
            problem.Extensions["errors"] = errors;
        if (links is not null)
            problem.Extensions["links"] = links;
        if (!string.IsNullOrWhiteSpace(traceId))
            problem.Extensions["traceId"] = traceId;

        return problem;
    }
}
