using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PersonalLibrary.Application.Catalog.Metadata;

namespace PersonalLibrary.Infrastructure.Books.Metadata;

internal sealed class WebSearchMetadataProvider(
    HttpClient httpClient,
    IOptions<BookMetadataOptions> options) : IBookMetadataSource
{
    private const string Instructions = """
        Search the web for bibliographic information associated with the supplied ISBN.

        Only return information supported by search results. Do not infer or invent missing
        values. If a field cannot be verified, return null. If you cannot reliably identify
        the book, set found to false, return null for nullable metadata fields, and return
        empty authors and sources arrays. Prefer publishers, booksellers, library catalogs,
        and other bibliographic sources. Every source URL must support the returned metadata.
        """;

    private static readonly JsonElement OutputSchema = CreateOutputSchema();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public string Name => "OpenAIWebSearch";
    public int Priority => 300;

    public async Task<BookMetadata?> GetByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default)
    {
        var normalizedIsbn = IsbnNormalizer.Normalize(isbn);
        var apiKey = options.Value.OpenAiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(options.Value.OpenAiModel))
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Post, "responses")
        {
            Content = JsonContent.Create(new
            {
                model = options.Value.OpenAiModel,
                instructions = Instructions,
                input = $"ISBN: {normalizedIsbn}",
                tools = new[] { new { type = "web_search", search_context_size = "medium" } },
                tool_choice = "required",
                include = new[] { "web_search_call.action.sources" },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "book_metadata",
                        strict = true,
                        schema = OutputSchema
                    }
                },
                store = false
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"OpenAI returned {(int)response.StatusCode} {response.StatusCode}: {responseBody}");
        }
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (!root.TryGetProperty("status", out var status) || status.GetString() != "completed")
            throw new JsonException("The web-search response did not complete.");

        var outputText = GetOutputText(root);
        if (outputText is null)
            return null;

        var result = JsonSerializer.Deserialize<WebSearchResult>(outputText, JsonOptions);
        if (result is null || !result.Found || string.IsNullOrWhiteSpace(result.Title) || result.Edition is null)
            return null;
        if (!MatchesRequestedIsbn(normalizedIsbn, result.Edition.Isbn10, result.Edition.Isbn13))
            return null;

        var sources = ExtractVerifiedSources(root);
        if (sources.Count == 0)
            return null;

        return new BookMetadata(
            result.Title.Trim(),
            Normalize(result.OriginalTitle),
            Normalize(result.Description),
            Normalize(result.LanguageCode),
            result.FirstPublishedYear is >= 1 and <= 9999 ? result.FirstPublishedYear : null,
            (result.Authors ?? [])
                .Where(author => !string.IsNullOrWhiteSpace(author.Name))
                .Select(author => new BookMetadataAuthor(author.Name!.Trim()))
                .ToList(),
            new BookEditionMetadata(
                NormalizeIsbn(result.Edition.Isbn10, 10),
                NormalizeIsbn(result.Edition.Isbn13, 13),
                Normalize(result.Edition.EditionName),
                Normalize(result.Edition.Publisher),
                Normalize(result.Edition.PublicationDate),
                result.Edition.PageCount is > 0 ? result.Edition.PageCount : null,
                Normalize(result.Edition.CoverImageUrl)),
            new BookMetadataProvenance(
                Name,
                root.TryGetProperty("id", out var responseId) ? responseId.GetString() : null,
                sources[0].Url,
                true,
                sources));
    }

    private static string? GetOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var itemType) || itemType.GetString() != "message" ||
                !item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var partType) &&
                    partType.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }

                if (part.TryGetProperty("type", out partType) && partType.GetString() == "refusal")
                    return null;
            }
        }

        return null;
    }

    private static List<BookMetadataSourceReference> ExtractVerifiedSources(JsonElement root)
    {
        var sources = new Dictionary<string, BookMetadataSourceReference>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return [];

        foreach (var item in output.EnumerateArray())
        {
            if (item.TryGetProperty("action", out var action) &&
                action.TryGetProperty("sources", out var actionSources) &&
                actionSources.ValueKind == JsonValueKind.Array)
            {
                foreach (var source in actionSources.EnumerateArray())
                    AddSource(source, sources);
            }

            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var part in content.EnumerateArray())
            {
                if (!part.TryGetProperty("annotations", out var annotations) ||
                    annotations.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var annotation in annotations.EnumerateArray())
                {
                    if (annotation.TryGetProperty("url_citation", out var citation))
                        AddSource(citation, sources);
                    else
                        AddSource(annotation, sources);
                }
            }
        }

        return sources.Values.ToList();
    }

    private static void AddSource(
        JsonElement source,
        IDictionary<string, BookMetadataSourceReference> sources)
    {
        if (!source.TryGetProperty("url", out var urlProperty))
            return;

        var url = urlProperty.GetString();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return;
        }

        var title = source.TryGetProperty("title", out var titleProperty)
            ? Normalize(titleProperty.GetString())
            : null;
        sources.TryAdd(uri.AbsoluteUri, new BookMetadataSourceReference(uri.AbsoluteUri, title));
    }

    private static bool MatchesRequestedIsbn(string requested, string? isbn10, string? isbn13)
    {
        var candidates = new[] { isbn10, isbn13 }
            .Where(value => IsbnNormalizer.TryNormalize(value, out _))
            .Select(value => IsbnNormalizer.Normalize(value))
            .ToHashSet(StringComparer.Ordinal);
        if (candidates.Contains(requested))
            return true;

        return requested.Length == 10
            ? candidates.Contains(ConvertIsbn10To13(requested))
            : candidates.Any(candidate => candidate.Length == 10 && ConvertIsbn10To13(candidate) == requested);
    }

    private static string ConvertIsbn10To13(string isbn10)
    {
        var firstTwelve = "978" + isbn10[..9];
        var sum = firstTwelve.Select((character, index) =>
            (character - '0') * (index % 2 == 0 ? 1 : 3)).Sum();
        return firstTwelve + ((10 - sum % 10) % 10);
    }

    private static string? NormalizeIsbn(string? value, int length) =>
        IsbnNormalizer.TryNormalize(value, out var normalized) && normalized!.Length == length
            ? normalized
            : null;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static JsonElement CreateOutputSchema()
    {
        using var document = JsonDocument.Parse("""
            {
              "type": "object",
              "additionalProperties": false,
              "properties": {
                "found": { "type": "boolean" },
                "title": { "type": ["string", "null"] },
                "originalTitle": { "type": ["string", "null"] },
                "description": { "type": ["string", "null"] },
                "languageCode": { "type": ["string", "null"] },
                "firstPublishedYear": { "type": ["integer", "null"] },
                "authors": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "additionalProperties": false,
                    "properties": { "name": { "type": "string" } },
                    "required": ["name"]
                  }
                },
                "edition": {
                  "anyOf": [
                    {
                      "type": "object",
                      "additionalProperties": false,
                      "properties": {
                        "isbn10": { "type": ["string", "null"] },
                        "isbn13": { "type": ["string", "null"] },
                        "editionName": { "type": ["string", "null"] },
                        "publisher": { "type": ["string", "null"] },
                        "publicationDate": { "type": ["string", "null"] },
                        "pageCount": { "type": ["integer", "null"] },
                        "coverImageUrl": { "type": ["string", "null"] }
                      },
                      "required": ["isbn10", "isbn13", "editionName", "publisher", "publicationDate", "pageCount", "coverImageUrl"]
                    },
                    { "type": "null" }
                  ]
                },
                "sources": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "additionalProperties": false,
                    "properties": {
                      "url": { "type": "string" },
                      "title": { "type": ["string", "null"] }
                    },
                    "required": ["url", "title"]
                  }
                }
              },
              "required": ["found", "title", "originalTitle", "description", "languageCode", "firstPublishedYear", "authors", "edition", "sources"]
            }
            """);
        return document.RootElement.Clone();
    }

    private sealed class WebSearchResult
    {
        public bool Found { get; init; }
        public string? Title { get; init; }
        public string? OriginalTitle { get; init; }
        public string? Description { get; init; }
        public string? LanguageCode { get; init; }
        public int? FirstPublishedYear { get; init; }
        public List<WebSearchAuthor>? Authors { get; init; }
        public WebSearchEdition? Edition { get; init; }
        public List<WebSearchSource>? Sources { get; init; }
    }

    private sealed class WebSearchAuthor
    {
        public string? Name { get; init; }
    }

    private sealed class WebSearchEdition
    {
        public string? Isbn10 { get; init; }
        public string? Isbn13 { get; init; }
        public string? EditionName { get; init; }
        public string? Publisher { get; init; }
        public string? PublicationDate { get; init; }
        public int? PageCount { get; init; }
        public string? CoverImageUrl { get; init; }
    }

    private sealed class WebSearchSource
    {
        public string? Url { get; init; }
        public string? Title { get; init; }
    }
}
