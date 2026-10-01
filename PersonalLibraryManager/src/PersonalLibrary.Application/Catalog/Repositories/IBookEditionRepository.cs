using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Catalog.Repositories
{
    public interface IBookEditionRepository : IRepository<BookEdition>
    {
        Task<BookEdition?> GetByIdWithDetailsAsync(Guid editionId, CancellationToken cancellationToken = default);
        Task<BookEdition?> GetByIsbnWithDetailsAsync(string isbn, CancellationToken cancellationToken = default);
        Task<List<BookEdition>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    }
}
