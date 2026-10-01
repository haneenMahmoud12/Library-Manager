using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Application.Catalog.Services;

public sealed class PublisherService(
    IPublisherRepository publisherRepository,
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser) : IPublisherService
{
    public async Task<PublisherViewModel> GetByIdAsync(Guid publisherId, CancellationToken cancellationToken = default)
    {
        CatalogRules.ValidateId(publisherId, "PublisherId");
        var publisher = await publisherRepository.GetByIdAsync(publisherId, cancellationToken)
            ?? throw new PublisherNotFoundException(publisherId);
        return Map(publisher);
    }

    public async Task<List<PublisherViewModel>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        CatalogRules.ValidateId(bookId, "BookId");
        if (!await bookRepository.ExistsAsync(bookId, cancellationToken))
            throw new BookNotFoundException(bookId);
        return (await publisherRepository.GetAllByBookIdAsync(bookId, cancellationToken)).Select(Map).ToList();
    }

    public async Task<PublisherViewModel> SaveAsync(CreatePublisherDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CatalogRules.RequireAuthenticatedUser(currentUser);
        var publisher = new Publisher
        {
            Id = Guid.NewGuid(),
            Name = CatalogRules.RequiredText(dto.Name, 300, "Name"),
            WebsiteUrl = CatalogRules.OptionalUrl(dto.WebsiteUrl, "WebsiteUrl"),
            CreatedAt = DateTime.UtcNow
        };
        publisherRepository.Add(publisher);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(publisher);
    }

    public async Task<PublisherViewModel> SaveAsync(Guid publisherId, UpdatePublisherDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CatalogRules.ValidateId(publisherId, "PublisherId");
        CatalogRules.RequireAuthenticatedUser(currentUser);
        if (dto.Name is null && dto.WebsiteUrl is null)
            throw new CatalogValidationException("Request", "At least one field must be supplied.");

        var publisher = await publisherRepository.GetByIdAsync(publisherId, cancellationToken)
            ?? throw new PublisherNotFoundException(publisherId);
        if (dto.Name is not null)
            publisher.Name = CatalogRules.RequiredText(dto.Name, 300, "Name");
        if (dto.WebsiteUrl is not null)
            publisher.WebsiteUrl = CatalogRules.OptionalUrl(dto.WebsiteUrl, "WebsiteUrl");
        publisher.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(publisher);
    }

    private static PublisherViewModel Map(Publisher publisher) =>
        new(publisher.Id, publisher.Name, publisher.WebsiteUrl, publisher.CreatedAt, publisher.UpdatedAt);
}
