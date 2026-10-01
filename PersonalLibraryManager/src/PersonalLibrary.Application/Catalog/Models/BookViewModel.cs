namespace PersonalLibrary.Application.Catalog.Models;

public sealed record BookViewModel(
    Guid Id,
    string Title,
    string? OriginalTitle,
    string? Description,
    string? OriginalLanguageCode,
    int? FirstPublishedYear,
    IReadOnlyCollection<BookAuthorViewModel> Authors,
    IReadOnlyCollection<BookEditionSummaryViewModel> Editions,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record BookAuthorViewModel(Guid Id, string Name, int AuthorOrder);

public sealed record BookEditionSummaryViewModel(
    Guid Id,
    Guid? PublisherId,
    string? Isbn10,
    string? Isbn13,
    string? EditionName,
    string? LanguageCode);
