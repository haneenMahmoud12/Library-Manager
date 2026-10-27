using System;
using System.Collections.Generic;
using System.Text;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Common.Pagination;

namespace PersonalLibrary.Application.Catalog.Services
{
    public interface IBookService
    {
        Task<BookViewModel> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default);
        Task<PagedResult<BookViewModel>> GetAllAsync(PageRequest page, CancellationToken cancellationToken = default);
        Task<List<BookViewModel>> GetAllByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default);
        Task<BookViewModel> SaveAsync(CreateBookDto bookDto, CancellationToken cancellationToken = default);
        Task<BookViewModel> SaveAsync(Guid bookId, UpdateBookDto bookDto, CancellationToken cancellationToken = default);
    }
}
