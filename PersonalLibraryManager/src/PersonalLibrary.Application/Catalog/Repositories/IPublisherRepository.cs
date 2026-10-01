using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Catalog.Repositories
{
    public interface IPublisherRepository : IRepository<Publisher>
    {
        Task<Publisher?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<List<Publisher>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    }
}
