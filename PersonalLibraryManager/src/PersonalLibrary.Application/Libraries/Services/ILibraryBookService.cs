using PersonalLibrary.Application.Libraries.Models;

namespace PersonalLibrary.Application.Libraries.Services;

public interface ILibraryBookService
{
    Task<LibraryBookViewModel> AddManuallyAsync(
        Guid libraryId,
        AddBookCopyDto dto,
        CancellationToken cancellationToken = default);

    Task<List<LibraryBookViewModel>> GetContentsAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default);
}
