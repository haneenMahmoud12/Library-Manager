using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories.Catalog;

internal sealed class BookRepository(ApplicationDbContext context)
    : Repository<Book>(context), IBookRepository
{
    public Task<Book?> GetByIdWithDetailsAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        DetailsQuery(false).SingleOrDefaultAsync(book => book.Id == bookId, cancellationToken);

    public Task<Book?> GetForUpdateAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        DetailsQuery(true).SingleOrDefaultAsync(book => book.Id == bookId, cancellationToken);

    public Task<List<Book>> GetAllByTitleForUpdateAsync(
        string title,
        CancellationToken cancellationToken = default) =>
        Context.Books
            .Include(book => book.BookAuthors)
                .ThenInclude(bookAuthor => bookAuthor.Author)
            .Where(book => book.Title == title)
            .ToListAsync(cancellationToken);

    public Task<List<Book>> GetAllByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default) =>
        DetailsQuery(false)
            .Where(book => book.BookAuthors.Any(bookAuthor => bookAuthor.AuthorId == authorId))
            .OrderBy(book => book.Title)
            .ToListAsync(cancellationToken);

    private IQueryable<Book> DetailsQuery(bool asTracking)
    {
        IQueryable<Book> query = Context.Books
            .Include(book => book.BookAuthors.OrderBy(bookAuthor => bookAuthor.AuthorOrder))
                .ThenInclude(bookAuthor => bookAuthor.Author)
            .Include(book => book.Editions);

        return asTracking ? query : query.AsNoTracking();
    }
}
