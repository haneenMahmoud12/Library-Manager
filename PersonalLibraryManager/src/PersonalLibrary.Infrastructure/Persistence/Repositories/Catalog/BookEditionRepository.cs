using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories.Catalog;

internal sealed class BookEditionRepository(ApplicationDbContext context)
    : Repository<BookEdition>(context), IBookEditionRepository
{
    public Task<BookEdition?> GetByIdWithDetailsAsync(
        Guid editionId,
        CancellationToken cancellationToken = default) =>
        DetailsQuery()
            .SingleOrDefaultAsync(edition => edition.Id == editionId, cancellationToken);

    public Task<BookEdition?> GetByIsbnWithDetailsAsync(
        string isbn,
        CancellationToken cancellationToken = default) =>
        DetailsQuery()
            .SingleOrDefaultAsync(
                edition => edition.Isbn10 == isbn || edition.Isbn13 == isbn,
                cancellationToken);

    private IQueryable<BookEdition> DetailsQuery() =>
        Context.BookEditions.AsNoTracking()
            .Include(edition => edition.Book)
                .ThenInclude(book => book.BookAuthors.OrderBy(bookAuthor => bookAuthor.AuthorOrder))
                    .ThenInclude(bookAuthor => bookAuthor.Author)
            .Include(edition => edition.Publisher);

    public Task<List<BookEdition>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        Context.BookEditions.AsNoTracking()
            .Where(edition => edition.BookId == bookId)
            .OrderBy(edition => edition.PublicationDate)
            .ThenBy(edition => edition.Id)
            .ToListAsync(cancellationToken);
}
