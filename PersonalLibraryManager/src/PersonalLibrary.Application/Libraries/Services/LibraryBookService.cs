using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Application.Libraries.Repositories;
using PersonalLibrary.Domain.Catalog;
using PersonalLibrary.Domain.Catalog.Enums;
using PersonalLibrary.Domain.Libraries;
using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Application.Libraries.Services;

public sealed class LibraryBookService(
    ILibraryRepository libraryRepository,
    IBookCopyRepository bookCopyRepository,
    IBookEditionRepository editionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser) : ILibraryBookService
{
    public async Task<LibraryBookViewModel> AddManuallyAsync(
        Guid libraryId,
        AddBookCopyDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateId(libraryId, "LibraryId");
        ValidateId(dto.BookEditionId, "BookEditionId");
        ValidateCopy(dto);

        var userId = GetRequiredUserId();
        var library = await libraryRepository.GetForUpdateAsync(libraryId, cancellationToken)
            ?? throw new LibraryNotFoundException(libraryId);
        EnsureAccess(library, userId, requireWriteAccess: true);

        var edition = await editionRepository.GetByIdWithDetailsAsync(dto.BookEditionId, cancellationToken)
            ?? throw new BookEditionNotFoundException(dto.BookEditionId);
        var location = GetLocation(library, dto.LocationId);

        var copy = new BookCopy
        {
            Id = Guid.NewGuid(),
            LibraryId = libraryId,
            BookEditionId = edition.Id,
            LocationId = location?.Id,
            AddedByUserId = userId,
            Format = dto.Format,
            ReadingStatus = dto.ReadingStatus,
            Condition = dto.Condition,
            AcquisitionDate = dto.AcquisitionDate,
            PurchasePrice = dto.PurchasePrice,
            CurrencyCode = NormalizeCurrency(dto.CurrencyCode),
            IsSigned = dto.IsSigned,
            IsFavourite = dto.IsFavourite,
            PersonalNotes = NormalizeOptional(dto.PersonalNotes),
            CreatedAt = DateTime.UtcNow
        };

        bookCopyRepository.Add(copy);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(copy, edition, location);
    }

    public async Task<List<LibraryBookViewModel>> GetContentsAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(libraryId, "LibraryId");
        var userId = GetRequiredUserId();
        var library = await libraryRepository.GetForUpdateAsync(libraryId, cancellationToken)
            ?? throw new LibraryNotFoundException(libraryId);
        EnsureAccess(library, userId, requireWriteAccess: false);

        var copies = await bookCopyRepository.GetAllByLibraryIdAsync(libraryId, cancellationToken);
        return copies.Select(Map).ToList();
    }

    private static void ValidateCopy(AddBookCopyDto dto)
    {
        if (!Enum.IsDefined(dto.Format))
            throw new LibraryValidationException("Format", "A valid book format is required.");
        if (!Enum.IsDefined(dto.ReadingStatus))
            throw new LibraryValidationException("ReadingStatus", "A valid reading status is required.");
        if (dto.Condition.HasValue && !Enum.IsDefined(dto.Condition.Value))
            throw new LibraryValidationException("Condition", "A valid book condition is required.");
        if (dto.PurchasePrice < 0)
            throw new LibraryValidationException("PurchasePrice", "Purchase price cannot be negative.");

        var currency = NormalizeCurrency(dto.CurrencyCode);
        if (dto.PurchasePrice.HasValue && currency is null)
            throw new LibraryValidationException("CurrencyCode", "Currency code is required when purchase price is supplied.");
    }

    private static string? NormalizeCurrency(string? value)
    {
        var currency = NormalizeOptional(value)?.ToUpperInvariant();
        if (currency is not null && (currency.Length != 3 || !currency.All(char.IsLetter)))
            throw new LibraryValidationException("CurrencyCode", "Currency code must contain exactly three letters.");
        return currency;
    }

    private static LibraryLocation? GetLocation(Library library, Guid? locationId)
    {
        if (!locationId.HasValue)
            return null;
        ValidateId(locationId.Value, "LocationId");
        return library.Locations.SingleOrDefault(location => location.Id == locationId.Value)
            ?? throw new LibraryValidationException("LocationId", "The location does not belong to this library.");
    }

    private static void EnsureAccess(Library library, Guid userId, bool requireWriteAccess)
    {
        var membership = library.Members.SingleOrDefault(member => member.UserId == userId)
            ?? throw new LibraryAccessDeniedException(library.Id, userId);
        if (requireWriteAccess && membership.Role is not (LibraryMemberRole.Owner or LibraryMemberRole.Editor))
            throw new LibraryAccessDeniedException(library.Id, userId);
    }

    private Guid GetRequiredUserId() =>
        currentUser.UserId is { } userId && userId != Guid.Empty
            ? userId
            : throw new UnauthorizedAccessException("An authenticated user is required.");

    private static void ValidateId(Guid id, string field)
    {
        if (id == Guid.Empty)
            throw new LibraryValidationException(field, $"A valid {field} is required.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LibraryBookViewModel Map(BookCopy copy) =>
        Map(copy, copy.BookEdition, copy.Location);

    private static LibraryBookViewModel Map(
        BookCopy copy,
        BookEdition edition,
        LibraryLocation? location)
    {
        var book = edition.Book;
        return new LibraryBookViewModel(
            copy.Id,
            copy.LibraryId,
            copy.BookEditionId,
            edition.BookId,
            book.Title,
            book.OriginalTitle,
            book.BookAuthors
                .OrderBy(bookAuthor => bookAuthor.AuthorOrder)
                .Select(bookAuthor => new LibraryBookAuthorViewModel(
                    bookAuthor.AuthorId,
                    bookAuthor.Author.Name,
                    bookAuthor.AuthorOrder))
                .ToList(),
            edition.EditionName,
            edition.Isbn10,
            edition.Isbn13,
            edition.LanguageCode,
            edition.PublisherId,
            edition.Publisher?.Name,
            copy.LocationId,
            location?.Name,
            copy.Format ?? throw new InvalidOperationException("A book copy must have a format."),
            copy.ReadingStatus,
            copy.Condition,
            copy.AcquisitionDate,
            copy.PurchasePrice,
            copy.CurrencyCode,
            copy.IsSigned,
            copy.IsFavourite,
            copy.PersonalNotes,
            copy.CreatedAt,
            copy.UpdatedAt);
    }
}
