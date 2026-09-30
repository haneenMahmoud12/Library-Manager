namespace PersonalLibrary.Api.Contracts.Catalog
{
    public class BookAuthorRequest
    {
        public required Guid AuthorId { get; init; }
        public int AuthorOrder { get; init; } = 1;
    }
}
