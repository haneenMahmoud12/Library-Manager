namespace PersonalLibrary.Application.Catalog.Metadata;

public sealed record BookMetadata(
    string Title,
    string? OriginalTitle,
    string? Description,
    string? LanguageCode,
    int? FirstPublishedYear,
    IReadOnlyList<BookMetadataAuthor> Authors,
    BookEditionMetadata Edition,
    BookMetadataProvenance Provenance);

public sealed record BookMetadataAuthor(
    string Name,
    string? ExternalId = null,
    string? Biography = null,
    string? BirthDate = null,
    string? DeathDate = null);

public sealed record BookEditionMetadata(
    string? Isbn10,
    string? Isbn13,
    string? EditionName,
    string? PublisherName,
    string? PublishedDate,
    int? PageCount,
    string? CoverImageUrl);

public sealed record BookMetadataProvenance(
    string Provider,
    string? ExternalId,
    string? InformationUrl,
    bool RequiresReview = false,
    IReadOnlyList<BookMetadataSourceReference>? Sources = null);

public sealed record BookMetadataSourceReference(string Url, string? Title = null);

public sealed record BookMetadataLookupResult(
    bool FoundInLocalCatalog,
    Guid? BookId,
    Guid? BookEditionId,
    bool RequiresReview,
    string? Warning,
    IReadOnlyList<BookMetadataSourceReference> Sources,
    BookMetadata Metadata);
