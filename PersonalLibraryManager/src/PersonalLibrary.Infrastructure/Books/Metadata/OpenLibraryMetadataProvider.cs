using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalLibrary.Application.Catalog.Metadata;

namespace PersonalLibrary.Infrastructure.Books.Metadata;

internal sealed class OpenLibraryMetadataProvider(HttpClient httpClient) : IBookMetadataSource
{
    public string Name => "OpenLibrary";
    public int Priority => 200;

    public async Task<BookMetadata?> GetByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default)
    {
        var normalizedIsbn = IsbnNormalizer.Normalize(isbn);
        using var response = await httpClient.GetAsync(
            $"isbn/{Uri.EscapeDataString(normalizedIsbn)}.json",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var edition = await response.Content.ReadFromJsonAsync<OpenLibraryEdition>(
            cancellationToken);
        if (edition is null || string.IsNullOrWhiteSpace(edition.Title))
            return null;

        var authors = await GetAuthorsAsync(edition.Authors, cancellationToken);
        var language = edition.Languages?
            .Select(reference => ExtractExternalId(reference.Key))
            .FirstOrDefault(value => value is not null);
        var coverId = edition.Covers?.FirstOrDefault();

        return new BookMetadata(
            edition.Title.Trim(),
            Normalize(edition.Subtitle),
            GetDescription(edition.Description),
            language,
            ParseYear(edition.PublishDate),
            authors,
            new BookEditionMetadata(
                FirstNormalized(edition.Isbn10),
                FirstNormalized(edition.Isbn13),
                Normalize(edition.EditionName),
                edition.Publishers?.Select(Normalize).FirstOrDefault(value => value is not null),
                Normalize(edition.PublishDate),
                edition.NumberOfPages,
                coverId is > 0
                    ? $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg"
                    : null),
            new BookMetadataProvenance(
                Name,
                ExtractExternalId(edition.Key),
                ToInformationUrl(edition.Key, normalizedIsbn)));
    }

    private async Task<IReadOnlyList<BookMetadataAuthor>> GetAuthorsAsync(
        IReadOnlyList<OpenLibraryReference>? references,
        CancellationToken cancellationToken)
    {
        if (references is null || references.Count == 0)
            return [];

        var authors = new List<BookMetadataAuthor>(references.Count);
        foreach (var reference in references.Where(reference => !string.IsNullOrWhiteSpace(reference.Key)))
        {
            var key = reference.Key!.TrimStart('/');
            using var response = await httpClient.GetAsync($"{key}.json", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                continue;

            response.EnsureSuccessStatusCode();
            var author = await response.Content.ReadFromJsonAsync<OpenLibraryAuthor>(cancellationToken);
            if (!string.IsNullOrWhiteSpace(author?.Name))
            {
                authors.Add(new BookMetadataAuthor(
                    author.Name.Trim(),
                    ExtractExternalId(reference.Key),
                    GetDescription(author.Bio),
                    Normalize(author.BirthDate),
                    Normalize(author.DeathDate)));
            }
        }

        return authors;
    }

    private static string? GetDescription(JsonElement? description)
    {
        if (!description.HasValue)
            return null;

        var value = description.Value;
        if (value.ValueKind == JsonValueKind.String)
            return Normalize(value.GetString());
        if (value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty("value", out var nestedValue) &&
            nestedValue.ValueKind == JsonValueKind.String)
        {
            return Normalize(nestedValue.GetString());
        }

        return null;
    }

    private static string? FirstNormalized(IEnumerable<string>? values)
    {
        if (values is null)
            return null;

        foreach (var value in values)
        {
            if (IsbnNormalizer.TryNormalize(value, out var normalized))
                return normalized;
        }

        return null;
    }

    private static int? ParseYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var fourDigits = new string(value.Where(char.IsDigit).Take(4).ToArray());
        return int.TryParse(fourDigits, out var year) ? year : null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ExtractExternalId(string? key) =>
        Normalize(key)?.Trim('/').Split('/').LastOrDefault();

    private static string ToInformationUrl(string? key, string isbn) =>
        string.IsNullOrWhiteSpace(key)
            ? $"https://openlibrary.org/isbn/{isbn}"
            : $"https://openlibrary.org/{key.TrimStart('/')}";

    private sealed class OpenLibraryEdition
    {
        [JsonPropertyName("key")] public string? Key { get; init; }
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("subtitle")] public string? Subtitle { get; init; }
        [JsonPropertyName("edition_name")] public string? EditionName { get; init; }
        [JsonPropertyName("publish_date")] public string? PublishDate { get; init; }
        [JsonPropertyName("number_of_pages")] public int? NumberOfPages { get; init; }
        [JsonPropertyName("description")] public JsonElement? Description { get; init; }
        [JsonPropertyName("authors")] public List<OpenLibraryReference>? Authors { get; init; }
        [JsonPropertyName("languages")] public List<OpenLibraryReference>? Languages { get; init; }
        [JsonPropertyName("publishers")] public List<string>? Publishers { get; init; }
        [JsonPropertyName("isbn_10")] public List<string>? Isbn10 { get; init; }
        [JsonPropertyName("isbn_13")] public List<string>? Isbn13 { get; init; }
        [JsonPropertyName("covers")] public List<long>? Covers { get; init; }
    }

    private sealed class OpenLibraryReference
    {
        [JsonPropertyName("key")] public string? Key { get; init; }
    }

    private sealed class OpenLibraryAuthor
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("bio")] public JsonElement? Bio { get; init; }
        [JsonPropertyName("birth_date")] public string? BirthDate { get; init; }
        [JsonPropertyName("death_date")] public string? DeathDate { get; init; }
    }
}
