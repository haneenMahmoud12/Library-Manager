using PersonalLibrary.Domain.Catalog.Enums;

namespace PersonalLibrary.Application.Libraries.Models;

public sealed record AddBookCopyDto(
    Guid BookEditionId,
    Guid? LocationId,
    BookFormat Format,
    ReadingStatus ReadingStatus,
    BookCondition? Condition,
    DateOnly? AcquisitionDate,
    decimal? PurchasePrice,
    string? CurrencyCode,
    bool? IsSigned,
    bool? IsFavourite,
    string? PersonalNotes);

public sealed record LibraryBookViewModel(
    Guid Id,
    Guid LibraryId,
    Guid BookEditionId,
    Guid BookId,
    string Title,
    string? OriginalTitle,
    IReadOnlyCollection<LibraryBookAuthorViewModel> Authors,
    string? EditionName,
    string? Isbn10,
    string? Isbn13,
    string? LanguageCode,
    Guid? PublisherId,
    string? PublisherName,
    Guid? LocationId,
    string? LocationName,
    BookFormat Format,
    ReadingStatus ReadingStatus,
    BookCondition? Condition,
    DateOnly? AcquisitionDate,
    decimal? PurchasePrice,
    string? CurrencyCode,
    bool? IsSigned,
    bool? IsFavourite,
    string? PersonalNotes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record LibraryBookAuthorViewModel(Guid Id, string Name, int AuthorOrder);

public sealed record BookOwnershipResult(
    string Isbn,
    bool IsOwned,
    bool OwnsDifferentEdition,
    int CopyCount,
    int DifferentEditionCopyCount,
    Guid? BookId,
    Guid? BookEditionId,
    IReadOnlyList<LibraryBookViewModel> Copies,
    IReadOnlyList<LibraryBookViewModel> DifferentEditionCopies)
{
    public bool CanDetermineOwnership => BookId.HasValue;

    public bool IsBookOwned => IsOwned || OwnsDifferentEdition;

    public string OwnershipStatus => !CanDetermineOwnership
        ? "UnknownEdition"
        : (IsOwned, OwnsDifferentEdition) switch
        {
            (true, true) => "ExactAndDifferentEditionsOwned",
            (true, false) => "ExactEditionOwned",
            (false, true) => "DifferentEditionOwned",
            _ => "NotOwned"
        };
}
