using PersonalLibrary.Application.Catalog.Models;

namespace PersonalLibrary.Application.Catalog.Services;

public interface IAuthorService
{
    Task<AuthorViewModel> GetByIdAsync(Guid authorId, CancellationToken cancellationToken = default);
    Task<List<AuthorViewModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<AuthorViewModel>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<AuthorViewModel> SaveAsync(CreateAuthorDto dto, CancellationToken cancellationToken = default);
    Task<AuthorViewModel> SaveAsync(Guid authorId, UpdateAuthorDto dto, CancellationToken cancellationToken = default);
}
