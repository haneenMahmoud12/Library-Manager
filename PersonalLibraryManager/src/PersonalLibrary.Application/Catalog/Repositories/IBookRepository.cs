using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Catalog.Repositories
{
    public interface IBookRepository : IRepository<Book>
    {
        Task<Book?> GetByIdWithDetailsAsync(Guid bookId, CancellationToken cancellationToken = default);
        Task<Book?> GetForUpdateAsync(Guid bookId, CancellationToken cancellationToken = default);
        Task<List<Book>> GetAllByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default);
    }
}
