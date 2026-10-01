namespace PersonalLibrary.Application.Catalog.Metadata;

public sealed record BookMetadata(
    string Title,
    string? Subtitle,
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
    string? InformationUrl);

public sealed record BookMetadataLookupResult(
    bool FoundInLocalCatalog,
    Guid? BookId,
    Guid? BookEditionId,
    BookMetadata Metadata);
