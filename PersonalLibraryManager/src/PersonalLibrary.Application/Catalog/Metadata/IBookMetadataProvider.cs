namespace PersonalLibrary.Application.Catalog.Metadata;

public interface IBookMetadataProvider
{
    Task<BookMetadata?> GetByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default);
}
