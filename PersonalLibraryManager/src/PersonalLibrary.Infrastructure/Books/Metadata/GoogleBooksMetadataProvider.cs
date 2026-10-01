using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PersonalLibrary.Application.Catalog.Metadata;

namespace PersonalLibrary.Infrastructure.Books.Metadata;

internal sealed class GoogleBooksMetadataProvider(
    HttpClient httpClient,
    IOptions<BookMetadataOptions> options) : IBookMetadataSource
{
    public string Name => "GoogleBooks";
    public int Priority => 100;

    public async Task<BookMetadata?> GetByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default)
    {
        var normalizedIsbn = IsbnNormalizer.Normalize(isbn);
        var apiKey = options.Value.GoogleBooksApiKey;
        var path = $"volumes?q=isbn%3A{Uri.EscapeDataString(normalizedIsbn)}&maxResults=5&projection=full";
        if (!string.IsNullOrWhiteSpace(apiKey))
            path += $"&key={Uri.EscapeDataString(apiKey)}";

        using var response = await httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GoogleVolumesResponse>(cancellationToken);

        var volume = payload?.Items?.FirstOrDefault(item =>
            item.VolumeInfo?.IndustryIdentifiers?.Any(identifier =>
                IsbnNormalizer.TryNormalize(identifier.Identifier, out var candidate) &&
                candidate == normalizedIsbn) == true);
        var info = volume?.VolumeInfo;
        if (volume is null || info is null || string.IsNullOrWhiteSpace(info.Title))
            return null;

        var identifiers = info.IndustryIdentifiers ?? [];
        return new BookMetadata(
            info.Title.Trim(),
            Normalize(info.Subtitle),
            Normalize(info.Description),
            Normalize(info.Language),
            ParseYear(info.PublishedDate),
            (info.Authors ?? [])
                .Where(author => !string.IsNullOrWhiteSpace(author))
                .Select(author => new BookMetadataAuthor(author.Trim()))
                .ToList(),
            new BookEditionMetadata(
                GetIdentifier(identifiers, "ISBN_10"),
                GetIdentifier(identifiers, "ISBN_13"),
                null,
                Normalize(info.Publisher),
                Normalize(info.PublishedDate),
                info.PageCount,
                ToHttps(info.ImageLinks?.Thumbnail ?? info.ImageLinks?.SmallThumbnail)),
            new BookMetadataProvenance(
                Name,
                volume.Id,
                string.IsNullOrWhiteSpace(volume.Id)
                    ? null
                    : $"https://books.google.com/books?id={Uri.EscapeDataString(volume.Id)}"));
    }

    private static string? GetIdentifier(IEnumerable<GoogleIndustryIdentifier> identifiers, string type) =>
        identifiers.FirstOrDefault(identifier =>
            string.Equals(identifier.Type, type, StringComparison.OrdinalIgnoreCase))?.Identifier;

    private static int? ParseYear(string? value) =>
        value?.Length >= 4 && int.TryParse(value[..4], out var year) ? year : null;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ToHttps(string? value) =>
        value?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true
            ? $"https://{value[7..]}"
            : value;

    private sealed class GoogleVolumesResponse
    {
        [JsonPropertyName("items")]
        public List<GoogleVolume>? Items { get; init; }
    }

    private sealed class GoogleVolume
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("volumeInfo")]
        public GoogleVolumeInfo? VolumeInfo { get; init; }
    }

    private sealed class GoogleVolumeInfo
    {
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("subtitle")] public string? Subtitle { get; init; }
        [JsonPropertyName("authors")] public List<string>? Authors { get; init; }
        [JsonPropertyName("publisher")] public string? Publisher { get; init; }
        [JsonPropertyName("publishedDate")] public string? PublishedDate { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
        [JsonPropertyName("industryIdentifiers")] public List<GoogleIndustryIdentifier>? IndustryIdentifiers { get; init; }
        [JsonPropertyName("pageCount")] public int? PageCount { get; init; }
        [JsonPropertyName("language")] public string? Language { get; init; }
        [JsonPropertyName("imageLinks")] public GoogleImageLinks? ImageLinks { get; init; }
    }

    private sealed class GoogleIndustryIdentifier
    {
        [JsonPropertyName("type")] public string? Type { get; init; }
        [JsonPropertyName("identifier")] public string? Identifier { get; init; }
    }

    private sealed class GoogleImageLinks
    {
        [JsonPropertyName("smallThumbnail")] public string? SmallThumbnail { get; init; }
        [JsonPropertyName("thumbnail")] public string? Thumbnail { get; init; }
    }
}
