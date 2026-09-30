namespace PersonalLibrary.Api.Contracts.Catalog
{
    public class SaveBookRequest
    {
        public required string Title { get; init; }
        public string? OriginalTitle { get; init; }
        public string? Description { get; init; }
        public string? OriginalLanguageCode { get; init; }
        public int? FirstPublishedYear { get; init; }

        public required IReadOnlyList<BookAuthorRequest> Authors { get; init; }
    }
}
