namespace PersonalLibrary.Application.Catalog.Exceptions;

public sealed class AuthorNotFoundException(Guid authorId)
    : Exception($"Author '{authorId}' was not found.")
{
    public Guid AuthorId { get; } = authorId;
}

public sealed class PublisherNotFoundException(Guid publisherId)
    : Exception($"Publisher '{publisherId}' was not found.")
{
    public Guid PublisherId { get; } = publisherId;
}

public sealed class BookEditionNotFoundException(Guid editionId)
    : Exception($"Book edition '{editionId}' was not found.")
{
    public Guid EditionId { get; } = editionId;
}
