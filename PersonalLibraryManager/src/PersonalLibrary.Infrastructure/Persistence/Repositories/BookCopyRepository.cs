using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Application.Libraries.Repositories;
using PersonalLibrary.Domain.Libraries;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories;

internal sealed class BookCopyRepository(ApplicationDbContext context)
    : Repository<BookCopy>(context), IBookCopyRepository
{
    public Task<List<BookCopy>> GetAllByLibraryIdAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default) =>
        Context.BookCopies.AsNoTracking()
            .Where(copy => copy.LibraryId == libraryId)
            .Include(copy => copy.BookEdition)
                .ThenInclude(edition => edition.Book)
                    .ThenInclude(book => book.BookAuthors.OrderBy(bookAuthor => bookAuthor.AuthorOrder))
                        .ThenInclude(bookAuthor => bookAuthor.Author)
            .Include(copy => copy.BookEdition)
                .ThenInclude(edition => edition.Publisher)
            .Include(copy => copy.Location)
            .OrderBy(copy => copy.BookEdition.Book.Title)
            .ThenBy(copy => copy.Id)
            .ToListAsync(cancellationToken);
}
