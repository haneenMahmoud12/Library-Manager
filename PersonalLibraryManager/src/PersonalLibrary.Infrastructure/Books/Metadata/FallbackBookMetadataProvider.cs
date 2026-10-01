using Microsoft.Extensions.Logging;
using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Catalog.Metadata;

namespace PersonalLibrary.Infrastructure.Books.Metadata;

internal sealed class FallbackBookMetadataProvider(
    IEnumerable<IBookMetadataSource> sources,
    ILogger<FallbackBookMetadataProvider> logger) : IBookMetadataProvider
{
    public async Task<BookMetadata?> GetByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default)
    {
        var normalizedIsbn = IsbnNormalizer.Normalize(isbn);
        Exception? lastFailure = null;
        var receivedResponse = false;

        foreach (var source in sources.OrderBy(source => source.Priority))
        {
            try
            {
                var result = await source.GetByIsbnAsync(normalizedIsbn, cancellationToken);
                receivedResponse = true;
                if (result is not null)
                    return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                lastFailure = exception;
                logger.LogWarning(
                    exception,
                    "Book metadata source {Source} failed for ISBN {Isbn}; trying the next source.",
                    source.Name,
                    normalizedIsbn);
            }
        }

        if (!receivedResponse && lastFailure is not null)
        {
            throw new BookMetadataProviderException(
                "All configured book metadata sources failed.",
                lastFailure);
        }

        return null;
    }
}
