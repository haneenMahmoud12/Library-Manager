namespace PersonalLibrary.Application.Catalog.Metadata;

public interface IBookMetadataSource
{
    string Name { get; }

    // Lower values run first. A future provider only needs to implement this
    // interface and be registered as IBookMetadataSource.
    int Priority { get; }

    Task<BookMetadata?> GetByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default);
}
