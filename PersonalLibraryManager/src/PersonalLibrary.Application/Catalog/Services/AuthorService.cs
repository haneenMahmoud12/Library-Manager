using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Application.Catalog.Services;

public sealed class AuthorService(
    IAuthorRepository authorRepository,
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser) : IAuthorService
{
    public async Task<AuthorViewModel> GetByIdAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        CatalogRules.ValidateId(authorId, "AuthorId");
        var author = await authorRepository.GetByIdAsync(authorId, cancellationToken)
            ?? throw new AuthorNotFoundException(authorId);
        return Map(author);
    }

    public async Task<PagedResult<AuthorViewModel>> GetAllAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        var result = await authorRepository.GetPageAsync(page, cancellationToken);
        return new PagedResult<AuthorViewModel>(result.Items.Select(Map).ToList(), result.PageNumber, result.PageSize, result.TotalCount);
    }

    public async Task<List<AuthorViewModel>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        CatalogRules.ValidateId(bookId, "BookId");
        if (!await bookRepository.ExistsAsync(bookId, cancellationToken))
            throw new BookNotFoundException(bookId);
        return (await authorRepository.GetAllByBookIdAsync(bookId, cancellationToken)).Select(Map).ToList();
    }

    public async Task<AuthorViewModel> SaveAsync(CreateAuthorDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CatalogRules.RequireAuthenticatedUser(currentUser);
        ValidateDates(dto.BirthDate, dto.DeathDate);

        var author = new Author
        {
            Id = Guid.NewGuid(),
            Name = CatalogRules.RequiredText(dto.Name, 300, "Name"),
            Biography = CatalogRules.OptionalText(dto.Biography, null, "Biography"),
            BirthDate = dto.BirthDate,
            DeathDate = dto.DeathDate,
            CreatedAt = DateTime.UtcNow
        };
        authorRepository.Add(author);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(author);
    }

    public async Task<AuthorViewModel> SaveAsync(Guid authorId, UpdateAuthorDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CatalogRules.ValidateId(authorId, "AuthorId");
        CatalogRules.RequireAuthenticatedUser(currentUser);

        if (dto.Name is null && dto.Biography is null && dto.BirthDate is null && dto.DeathDate is null)
            throw new CatalogValidationException("Request", "At least one field must be supplied.");

        var author = await authorRepository.GetByIdAsync(authorId, cancellationToken)
            ?? throw new AuthorNotFoundException(authorId);
        var birthDate = dto.BirthDate ?? author.BirthDate;
        var deathDate = dto.DeathDate ?? author.DeathDate;
        ValidateDates(birthDate, deathDate);

        if (dto.Name is not null)
            author.Name = CatalogRules.RequiredText(dto.Name, 300, "Name");
        if (dto.Biography is not null)
            author.Biography = CatalogRules.OptionalText(dto.Biography, null, "Biography");
        if (dto.BirthDate.HasValue)
            author.BirthDate = dto.BirthDate;
        if (dto.DeathDate.HasValue)
            author.DeathDate = dto.DeathDate;
        author.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(author);
    }

    private static void ValidateDates(DateOnly? birthDate, DateOnly? deathDate)
    {
        if (birthDate.HasValue && deathDate.HasValue && deathDate < birthDate)
            throw new CatalogValidationException("DeathDate", "Death date cannot be before birth date.");
    }

    private static AuthorViewModel Map(Author author) =>
        new(author.Id, author.Name, author.Biography, author.BirthDate, author.DeathDate, author.CreatedAt, author.UpdatedAt);
}
