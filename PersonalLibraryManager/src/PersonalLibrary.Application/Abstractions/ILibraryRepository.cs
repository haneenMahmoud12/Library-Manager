using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Libraries;

namespace PersonalLibrary.Application.Abstractions;

public interface ILibraryRepository : IRepository<Library>
{
    Task<Library?> GetForUpdateAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default);

    Task<Library?> GetByIdWithDetailsAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<LibraryListItemViewModel>> GetUserLibrariesAsync(
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken = default);
}
