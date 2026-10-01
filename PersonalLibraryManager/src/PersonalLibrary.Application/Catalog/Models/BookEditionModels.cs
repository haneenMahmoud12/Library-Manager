namespace PersonalLibrary.Application.Catalog.Models;

public sealed record CreateBookEditionDto(
    Guid BookId,
    Guid? PublisherId,
    string? Isbn10,
    string? Isbn13,
    string? EditionName,
    string? LanguageCode,
    DateOnly? PublicationDate,
    int? PageCount,
    string? CoverImageUrl,
    string? Description);

public sealed record UpdateBookEditionDto(
    Guid? BookId,
    Guid? PublisherId,
    string? Isbn10,
    string? Isbn13,
    string? EditionName,
    string? LanguageCode,
    DateOnly? PublicationDate,
    int? PageCount,
    string? CoverImageUrl,
    string? Description);

public sealed record BookEditionViewModel(
    Guid Id,
    Guid BookId,
    Guid? PublisherId,
    string? Isbn10,
    string? Isbn13,
    string? EditionName,
    string? LanguageCode,
    DateOnly? PublicationDate,
    int? PageCount,
    string? CoverImageUrl,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
