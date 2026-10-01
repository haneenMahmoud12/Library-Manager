using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Domain.Libraries;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories;

internal sealed class LibraryRepository(ApplicationDbContext context)
    : Repository<Library>(context), ILibraryRepository
{
    public Task<Library?> GetForUpdateAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default) =>
        Context.Libraries
            .Include(library => library.Members)
            .Include(library => library.Locations)
            .SingleOrDefaultAsync(
                library => library.Id == libraryId,
                cancellationToken);

    public Task<Library?> GetByIdWithDetailsAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default) =>
        Context.Libraries
            .AsNoTracking()
            .Include(library => library.Members)
            .Include(library => library.BookCopies)
            .SingleOrDefaultAsync(library => library.Id == libraryId, cancellationToken);

    public async Task<PagedResult<LibraryListItemViewModel>> GetUserLibrariesAsync(
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        var libraries = Context.Libraries
            .AsNoTracking()
            .Where(library => library.Members.Any(member => member.UserId == userId));

        if (page.SearchText is not null)
        {
            var search = page.SearchText;
            libraries = libraries.Where(library =>
                library.Name.Contains(search) ||
                (library.Description != null && library.Description.Contains(search)));
        }

        var totalCount = await libraries.LongCountAsync(cancellationToken);
        var items = await ApplyOrdering(libraries, page, userId)
            .Skip(page.Offset)
            .Take(page.PageSize)
            .Select(library => new LibraryListItemViewModel(
                library.Id,
                library.Name,
                library.Description,
                library.Visibility,
                library.Members
                    .Where(member => member.UserId == userId)
                    .Select(member => member.Role)
                    .Single(),
                library.Members.Count,
                library.BookCopies.Count,
                library.CreatedAt,
                library.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<LibraryListItemViewModel>(
            items,
            page.PageNumber,
            page.PageSize,
            totalCount);
    }

    private static IOrderedQueryable<Library> ApplyOrdering(
        IQueryable<Library> query,
        PageRequest page,
        Guid userId)
    {
        var descending = page.OrderDirection == OrderDirection.Descending;

        IOrderedQueryable<Library> ordered = page.OrderBy.ToUpperInvariant() switch
        {
            "NAME" => descending
                ? query.OrderByDescending(library => library.Name)
                : query.OrderBy(library => library.Name),
            "VISIBILITY" => descending
                ? query.OrderByDescending(library => library.Visibility)
                : query.OrderBy(library => library.Visibility),
            "CURRENTUSERROLE" => descending
                ? query.OrderByDescending(library => library.Members
                    .Where(member => member.UserId == userId)
                    .Select(member => member.Role)
                    .Single())
                : query.OrderBy(library => library.Members
                    .Where(member => member.UserId == userId)
                    .Select(member => member.Role)
                    .Single()),
            "MEMBERCOUNT" => descending
                ? query.OrderByDescending(library => library.Members.Count)
                : query.OrderBy(library => library.Members.Count),
            "BOOKCOUNT" => descending
                ? query.OrderByDescending(library => library.BookCopies.Count)
                : query.OrderBy(library => library.BookCopies.Count),
            "CREATEDAT" => descending
                ? query.OrderByDescending(library => library.CreatedAt)
                : query.OrderBy(library => library.CreatedAt),
            "UPDATEDAT" => descending
                ? query.OrderByDescending(library => library.UpdatedAt)
                : query.OrderBy(library => library.UpdatedAt),
            _ => descending
                ? query.OrderByDescending(library => library.Id)
                : query.OrderBy(library => library.Id)
        };

        return page.OrderBy.Equals("Id", StringComparison.OrdinalIgnoreCase)
            ? ordered
            : ordered.ThenBy(library => library.Id);
    }
}
