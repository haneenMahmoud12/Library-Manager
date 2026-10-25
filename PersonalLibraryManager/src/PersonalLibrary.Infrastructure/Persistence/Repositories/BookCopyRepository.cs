using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Application.Libraries.Repositories;
using PersonalLibrary.Domain.Libraries;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories;

internal sealed class BookCopyRepository(ApplicationDbContext context)
    : Repository<BookCopy>(context), IBookCopyRepository
{
    public Task<BookCopy?> GetByIdWithDetailsAsync(
        Guid bookCopyId,
        CancellationToken cancellationToken = default) =>
        WithDetails()
            .SingleOrDefaultAsync(copy => copy.Id == bookCopyId, cancellationToken);

    public Task<List<BookCopy>> GetAllByLibraryIdAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default) =>
        WithDetails()
            .Where(copy => copy.LibraryId == libraryId)
            .OrderBy(copy => copy.BookEdition.Book.Title)
            .ThenBy(copy => copy.Id)
            .ToListAsync(cancellationToken);

    public Task<List<BookCopy>> GetAllByLibraryAndBookEditionIdAsync(
        Guid libraryId,
        Guid bookEditionId,
        CancellationToken cancellationToken = default) =>
        WithDetails()
            .Where(copy =>
                copy.LibraryId == libraryId &&
                copy.BookEditionId == bookEditionId)
            .OrderBy(copy => copy.Id)
            .ToListAsync(cancellationToken);

    private IQueryable<BookCopy> WithDetails() =>
        Context.BookCopies.AsNoTracking()
            .Include(copy => copy.BookEdition)
                .ThenInclude(edition => edition.Book)
                    .ThenInclude(book => book.BookAuthors.OrderBy(bookAuthor => bookAuthor.AuthorOrder))
                        .ThenInclude(bookAuthor => bookAuthor.Author)
            .Include(copy => copy.BookEdition)
                .ThenInclude(edition => edition.Publisher)
            .Include(copy => copy.Location);
}
