namespace PersonalLibrary.Api.Contracts.Catalog;

public sealed class CreateBookEditionRequest
{
    public Guid BookId { get; init; }
    public Guid? PublisherId { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    public string? EditionName { get; init; }
    public string? LanguageCode { get; init; }
    public DateOnly? PublicationDate { get; init; }
    public int? PageCount { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? Description { get; init; }
}

public sealed class UpdateBookEditionRequest
{
    public Guid? BookId { get; init; }
    public Guid? PublisherId { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    public string? EditionName { get; init; }
    public string? LanguageCode { get; init; }
    public DateOnly? PublicationDate { get; init; }
    public int? PageCount { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? Description { get; init; }
}
