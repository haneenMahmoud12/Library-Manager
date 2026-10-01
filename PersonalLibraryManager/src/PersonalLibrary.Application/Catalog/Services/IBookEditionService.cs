using PersonalLibrary.Application.Catalog.Models;

namespace PersonalLibrary.Application.Catalog.Services;

public interface IBookEditionService
{
    Task<BookEditionViewModel> GetByIdAsync(Guid editionId, CancellationToken cancellationToken = default);
    Task<List<BookEditionViewModel>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<BookEditionViewModel> SaveAsync(CreateBookEditionDto dto, CancellationToken cancellationToken = default);
    Task<BookEditionViewModel> SaveAsync(Guid editionId, UpdateBookEditionDto dto, CancellationToken cancellationToken = default);
}
