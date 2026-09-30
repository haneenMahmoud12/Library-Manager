using System;
using System.Collections.Generic;
using System.Text;
using PersonalLibrary.Application.Catalog.Models;

namespace PersonalLibrary.Application.Catalog.Services
{
    public interface IBookService
    {
        Task<BookViewModel?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken);
        Task<BookViewModel> SaveAsync(CreateBookDto bookDto, CancellationToken cancellationToken);
        Task<BookViewModel> SaveAsync(Guid bookId, UpdateBookDto bookDto, CancellationToken cancellationToken);
    }
}
