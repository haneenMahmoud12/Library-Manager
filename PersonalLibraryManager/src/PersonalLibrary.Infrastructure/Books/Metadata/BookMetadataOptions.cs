namespace PersonalLibrary.Infrastructure.Books.Metadata;

public sealed class BookMetadataOptions
{
    public const string SectionName = "BookMetadata";

    public string? GoogleBooksApiKey { get; init; }
    public int RequestTimeoutSeconds { get; init; } = 10;
}
