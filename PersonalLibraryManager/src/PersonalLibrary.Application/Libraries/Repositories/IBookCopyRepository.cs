using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Catalog;
using PersonalLibrary.Domain.Libraries;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Libraries.Repositories
{
    public interface IBookCopyRepository : IRepository<BookCopy>
    {
        Task<BookCopy?> GetByIdWithDetailsAsync(
            Guid bookCopyId,
            CancellationToken cancellationToken = default);

        Task<List<BookCopy>> GetAllByLibraryIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default);

        Task<List<BookCopy>> GetAllByLibraryAndBookEditionIdAsync(
            Guid libraryId,
            Guid bookEditionId,
            CancellationToken cancellationToken = default);
    }
}
