namespace PersonalLibrary.Infrastructure.Books.Metadata;

public sealed class BookMetadataOptions
{
    public const string SectionName = "BookMetadata";

    public string? GoogleBooksApiKey { get; init; }
    public string? OpenAiApiKey { get; init; }
    public string OpenAiModel { get; init; } = "gpt-5.5";
    public int RequestTimeoutSeconds { get; init; } = 10;
    public int WebSearchTimeoutSeconds { get; init; } = 45;
}
