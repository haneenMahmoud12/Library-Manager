using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories.Catalog;

internal sealed class AuthorRepository(ApplicationDbContext context)
    : Repository<Author>(context), IAuthorRepository
{
    public Task<List<Author>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        Context.BookAuthors.AsNoTracking()
            .Where(bookAuthor => bookAuthor.BookId == bookId)
            .OrderBy(bookAuthor => bookAuthor.AuthorOrder)
            .Select(bookAuthor => bookAuthor.Author)
            .ToListAsync(cancellationToken);
}
