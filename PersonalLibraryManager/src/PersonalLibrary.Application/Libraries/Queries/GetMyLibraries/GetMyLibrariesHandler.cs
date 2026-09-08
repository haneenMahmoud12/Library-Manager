using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Libraries.Repositories;

namespace PersonalLibrary.Application.Libraries.Queries.GetMyLibraries;

public sealed class GetMyLibrariesHandler(
    ILibraryRepository libraryRepository,
    ICurrentUserContext currentUser)
{
    private static readonly HashSet<string> AllowedOrderColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "Name",
            "Visibility",
            "CurrentUserRole",
            "MemberCount",
            "BookCount",
            "CreatedAt",
            "UpdatedAt"
        };

    public async Task<PagedResult<GetMyLibrariesResult>> HandleAsync(
        GetMyLibrariesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Page);

        var userId = currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            throw new UnauthorizedAccessException("An authenticated user is required.");

        if (!AllowedOrderColumns.Contains(query.Page.OrderBy))
        {
            throw new LibraryValidationException(
                "OrderBy",
                $"OrderBy must be one of: {string.Join(", ", AllowedOrderColumns)}.");
        }

        return await libraryRepository.GetUserLibrariesAsync(
            userId.Value,
            query.Page,
            cancellationToken);
    }
}
