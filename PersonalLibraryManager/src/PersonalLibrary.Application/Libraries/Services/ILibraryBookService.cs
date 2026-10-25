using PersonalLibrary.Application.Libraries.Models;

namespace PersonalLibrary.Application.Libraries.Services;

public interface ILibraryBookService
{
    Task<LibraryBookViewModel> AddManuallyAsync(
        Guid libraryId,
        AddBookCopyDto dto,
        CancellationToken cancellationToken = default);

    Task<LibraryBookViewModel> GetByIdAsync(
        Guid libraryId,
        Guid bookCopyId,
        CancellationToken cancellationToken = default);

    Task<List<LibraryBookViewModel>> GetContentsAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default);

    Task<BookOwnershipResult> CheckOwnershipAsync(
        Guid libraryId,
        string isbn,
        CancellationToken cancellationToken = default);

    Task<LibraryBookViewModel> ImportAsync(
        Guid libraryId,
        string isbn,
        ImportBookToLibraryDto request,
        CancellationToken cancellationToken = default);
}
