using PersonalLibrary.Application.Catalog.Models;

namespace PersonalLibrary.Application.Catalog.Services;

public interface IPublisherService
{
    Task<PublisherViewModel> GetByIdAsync(Guid publisherId, CancellationToken cancellationToken = default);
    Task<List<PublisherViewModel>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<PublisherViewModel> SaveAsync(CreatePublisherDto dto, CancellationToken cancellationToken = default);
    Task<PublisherViewModel> SaveAsync(Guid publisherId, UpdatePublisherDto dto, CancellationToken cancellationToken = default);
}
