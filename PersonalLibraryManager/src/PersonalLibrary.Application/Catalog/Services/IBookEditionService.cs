using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Common.Pagination;

namespace PersonalLibrary.Application.Catalog.Services;

public interface IBookEditionService
{
    Task<BookEditionViewModel> GetByIdAsync(Guid editionId, CancellationToken cancellationToken = default);
    Task<PagedResult<BookEditionViewModel>> GetAllAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<List<BookEditionViewModel>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<BookEditionViewModel> SaveAsync(CreateBookEditionDto dto, CancellationToken cancellationToken = default);
    Task<BookEditionViewModel> SaveAsync(Guid editionId, UpdateBookEditionDto dto, CancellationToken cancellationToken = default);
}
