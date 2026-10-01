using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Catalog.Repositories
{
    public interface IAuthorRepository : IRepository<Author>
    {
        Task<Author?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<List<Author>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    }
}
