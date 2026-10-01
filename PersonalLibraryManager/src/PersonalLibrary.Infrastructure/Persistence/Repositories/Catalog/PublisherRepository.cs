using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories.Catalog;

internal sealed class PublisherRepository(ApplicationDbContext context)
    : Repository<Publisher>(context), IPublisherRepository
{
    public Task<List<Publisher>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        Context.BookEditions.AsNoTracking()
            .Where(edition => edition.BookId == bookId && edition.Publisher != null)
            .Select(edition => edition.Publisher!)
            .Distinct()
            .OrderBy(publisher => publisher.Name)
            .ToListAsync(cancellationToken);
}
