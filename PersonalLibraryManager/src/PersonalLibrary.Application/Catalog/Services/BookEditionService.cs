using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Application.Catalog.Services;

public sealed class BookEditionService(
    IBookEditionRepository editionRepository,
    IBookRepository bookRepository,
    IPublisherRepository publisherRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser) : IBookEditionService
{
    public async Task<BookEditionViewModel> GetByIdAsync(Guid editionId, CancellationToken cancellationToken = default)
    {
        CatalogRules.ValidateId(editionId, "EditionId");
        var edition = await editionRepository.GetByIdAsync(editionId, cancellationToken)
            ?? throw new BookEditionNotFoundException(editionId);
        return Map(edition);
    }

    public async Task<List<BookEditionViewModel>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        CatalogRules.ValidateId(bookId, "BookId");
        if (!await bookRepository.ExistsAsync(bookId, cancellationToken))
            throw new BookNotFoundException(bookId);
        return (await editionRepository.GetAllByBookIdAsync(bookId, cancellationToken)).Select(Map).ToList();
    }

    public async Task<BookEditionViewModel> SaveAsync(CreateBookEditionDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CatalogRules.RequireAuthenticatedUser(currentUser);
        await ValidateRelationsAsync(dto.BookId, dto.PublisherId, cancellationToken);

        var edition = new BookEdition
        {
            Id = Guid.NewGuid(),
            BookId = dto.BookId,
            PublisherId = dto.PublisherId,
            Isbn10 = ValidateIsbn10(dto.Isbn10),
            Isbn13 = ValidateIsbn13(dto.Isbn13),
            EditionName = CatalogRules.OptionalText(dto.EditionName, 200, "EditionName"),
            LanguageCode = CatalogRules.OptionalText(dto.LanguageCode, 10, "LanguageCode"),
            PublicationDate = dto.PublicationDate,
            PageCount = ValidatePageCount(dto.PageCount),
            CoverImageUrl = CatalogRules.OptionalUrl(dto.CoverImageUrl, "CoverImageUrl"),
            Description = CatalogRules.OptionalText(dto.Description, null, "Description"),
            CreatedAt = DateTime.UtcNow
        };
        editionRepository.Add(edition);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(edition);
    }

    public async Task<BookEditionViewModel> SaveAsync(Guid editionId, UpdateBookEditionDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CatalogRules.ValidateId(editionId, "EditionId");
        CatalogRules.RequireAuthenticatedUser(currentUser);
        if (AllFieldsMissing(dto))
            throw new CatalogValidationException("Request", "At least one field must be supplied.");

        var edition = await editionRepository.GetByIdAsync(editionId, cancellationToken)
            ?? throw new BookEditionNotFoundException(editionId);

        var bookId = dto.BookId ?? edition.BookId;
        var publisherId = dto.PublisherId == Guid.Empty ? null : dto.PublisherId ?? edition.PublisherId;
        await ValidateRelationsAsync(bookId, publisherId, cancellationToken);

        if (dto.BookId.HasValue)
            edition.BookId = dto.BookId.Value;
        if (dto.PublisherId.HasValue)
            edition.PublisherId = dto.PublisherId == Guid.Empty ? null : dto.PublisherId;
        if (dto.Isbn10 is not null)
            edition.Isbn10 = ValidateIsbn10(dto.Isbn10);
        if (dto.Isbn13 is not null)
            edition.Isbn13 = ValidateIsbn13(dto.Isbn13);
        if (dto.EditionName is not null)
            edition.EditionName = CatalogRules.OptionalText(dto.EditionName, 200, "EditionName");
        if (dto.LanguageCode is not null)
            edition.LanguageCode = CatalogRules.OptionalText(dto.LanguageCode, 10, "LanguageCode");
        if (dto.PublicationDate.HasValue)
            edition.PublicationDate = dto.PublicationDate;
        if (dto.PageCount.HasValue)
            edition.PageCount = ValidatePageCount(dto.PageCount);
        if (dto.CoverImageUrl is not null)
            edition.CoverImageUrl = CatalogRules.OptionalUrl(dto.CoverImageUrl, "CoverImageUrl");
        if (dto.Description is not null)
            edition.Description = CatalogRules.OptionalText(dto.Description, null, "Description");
        edition.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(edition);
    }

    private async Task ValidateRelationsAsync(Guid bookId, Guid? publisherId, CancellationToken cancellationToken)
    {
        CatalogRules.ValidateId(bookId, "BookId");
        if (!await bookRepository.ExistsAsync(bookId, cancellationToken))
            throw new BookNotFoundException(bookId);
        if (publisherId.HasValue)
        {
            CatalogRules.ValidateId(publisherId.Value, "PublisherId");
            if (!await publisherRepository.ExistsAsync(publisherId.Value, cancellationToken))
                throw new PublisherNotFoundException(publisherId.Value);
        }
    }

    private static bool AllFieldsMissing(UpdateBookEditionDto dto) =>
        dto.BookId is null && dto.PublisherId is null && dto.Isbn10 is null && dto.Isbn13 is null &&
        dto.EditionName is null && dto.LanguageCode is null && dto.PublicationDate is null &&
        dto.PageCount is null && dto.CoverImageUrl is null && dto.Description is null;

    private static string? ValidateIsbn10(string? value)
    {
        var isbn = NormalizeIsbn(value);
        if (isbn is not null && (isbn.Length != 10 ||
            !isbn[..9].All(char.IsDigit) || !(char.IsDigit(isbn[9]) || isbn[9] is 'X' or 'x')))
            throw new CatalogValidationException("Isbn10", "ISBN-10 must contain 10 digits (the final character may be X).");
        return isbn?.ToUpperInvariant();
    }

    private static string? ValidateIsbn13(string? value)
    {
        var isbn = NormalizeIsbn(value);
        if (isbn is not null && (isbn.Length != 13 || !isbn.All(char.IsDigit)))
            throw new CatalogValidationException("Isbn13", "ISBN-13 must contain exactly 13 digits.");
        return isbn;
    }

    private static string? NormalizeIsbn(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().Replace("-", string.Empty).Replace(" ", string.Empty);

    private static int? ValidatePageCount(int? pageCount)
    {
        if (pageCount is <= 0)
            throw new CatalogValidationException("PageCount", "Page count must be greater than zero.");
        return pageCount;
    }

    private static BookEditionViewModel Map(BookEdition edition) =>
        new(
            edition.Id, edition.BookId, edition.PublisherId, edition.Isbn10, edition.Isbn13,
            edition.EditionName, edition.LanguageCode, edition.PublicationDate, edition.PageCount,
            edition.CoverImageUrl, edition.Description, edition.CreatedAt, edition.UpdatedAt);
}
