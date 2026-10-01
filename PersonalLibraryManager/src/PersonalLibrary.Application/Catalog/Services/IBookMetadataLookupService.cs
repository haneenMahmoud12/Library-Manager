using PersonalLibrary.Application.Catalog.Metadata;

namespace PersonalLibrary.Application.Catalog.Services;

public interface IBookMetadataLookupService
{
    Task<BookMetadataLookupResult> GetByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default);
}
