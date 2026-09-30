using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Models;

namespace PersonalLibrary.Application.Libraries.Services;

public interface ILibraryService
{
    Task<LibraryViewModel> SaveAsync(
        CreateLibraryDto dto,
        CancellationToken cancellationToken = default);

    Task<LibraryViewModel> SaveAsync(
        Guid libraryId,
        UpdateLibraryDto dto,
        CancellationToken cancellationToken = default);

    Task<LibraryViewModel> GetByIdAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<LibraryListItemViewModel>> GetMyLibrariesAsync(
        PageRequest page,
        CancellationToken cancellationToken = default);
}
