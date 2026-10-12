using PersonalLibrary.Application.Catalog.Metadata;

namespace PersonalLibrary.Application.Catalog.Services;

public interface IBookMetadataLookupService
{
    Task<BookMetadataLookupResult> PreviewByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default);

    Task<BookMetadataLookupResult> ImportAsync(
        string isbn,
        BookMetadata metadata,
        bool confirmWebMetadata,
        CancellationToken cancellationToken = default);
}
