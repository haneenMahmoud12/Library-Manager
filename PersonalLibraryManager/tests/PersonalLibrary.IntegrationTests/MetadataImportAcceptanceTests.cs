using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Libraries;
using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Infrastructure.Persistence;
using PersonalLibrary.IntegrationTests.Identity;

namespace PersonalLibrary.IntegrationTests;

public sealed class MetadataImportAcceptanceTests(RegistrationApiFactory factory)
    : IClassFixture<RegistrationApiFactory>
{
    [Fact]
    public async Task No_result_response_offers_manual_book_and_edition_entry()
    {
        const string isbn = "9780307277671";
        factory.Metadata.SetResult(isbn, null);
        var authenticated = await IdentityAcceptanceTests.CreateAuthenticatedClientAsync(factory);
        using var client = authenticated.Client;

        var response = await client.GetAsync($"/api/book-metadata/{isbn}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal("BookMetadataNotFound", root.GetProperty("code").GetString());
        Assert.Equal("/api/books", root.GetProperty("links").GetProperty("createBook").GetProperty("href").GetString());
        Assert.Equal("/api/book-editions", root.GetProperty("links").GetProperty("createEdition").GetProperty("href").GetString());
    }

    [Fact]
    public async Task Web_metadata_preview_exposes_one_source_and_requires_explicit_confirmation()
    {
        const string isbn = "9780451524935";
        var metadata = WebMetadata(isbn, "Provenance Acceptance Book", "Provenance Author");
        factory.Metadata.SetResult(isbn, metadata);
        var authenticated = await IdentityAcceptanceTests.CreateAuthenticatedClientAsync(factory);
        using var client = authenticated.Client;

        var preview = await GetDataAsync<BookMetadataLookupResult>(client, $"/api/book-metadata/{isbn}");
        Assert.False(preview.FoundInLocalCatalog);
        Assert.True(preview.RequiresReview);
        Assert.Single(preview.Sources);
        Assert.Equal("https://example.com/provenance-book", preview.Sources[0].Url);
        Assert.Equal("OpenAIWebSearch", preview.Metadata.Provenance.Provider);

        var unconfirmed = await client.PutAsJsonAsync(
            $"/api/book-editions/by-isbn/{isbn}",
            new ImportBookMetadataRequest { Metadata = metadata, ConfirmWebMetadata = false });
        Assert.Equal(HttpStatusCode.BadRequest, unconfirmed.StatusCode);

        var confirmed = await client.PutAsJsonAsync(
            $"/api/book-editions/by-isbn/{isbn}",
            new ImportBookMetadataRequest { Metadata = metadata, ConfirmWebMetadata = true });
        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
    }

    [Fact]
    public async Task Reviewed_import_is_atomic_and_reuses_book_author_and_publisher_entities()
    {
        const string firstIsbn = "9780679783275";
        const string secondIsbn = "9780140449136";
        const string title = "Atomic Import Acceptance Book";
        const string author = "Atomic Import Author";
        const string publisher = "Atomic Import Press";
        var authenticated = await IdentityAcceptanceTests.CreateAuthenticatedClientAsync(factory);
        using var client = authenticated.Client;

        var firstMetadata = WebMetadata(firstIsbn, title, author, publisher);
        var first = await PutDataAsync<BookMetadataLookupResult>(
            client,
            $"/api/book-editions/by-isbn/{firstIsbn}",
            new ImportBookMetadataRequest { Metadata = firstMetadata, ConfirmWebMetadata = true },
            HttpStatusCode.Created);

        var secondMetadata = WebMetadata(secondIsbn, title, author, publisher);
        var second = await PutDataAsync<BookMetadataLookupResult>(
            client,
            $"/api/book-editions/by-isbn/{secondIsbn}",
            new ImportBookMetadataRequest { Metadata = secondMetadata, ConfirmWebMetadata = true },
            HttpStatusCode.Created);

        Assert.Equal(first.BookId, second.BookId);
        Assert.NotEqual(first.BookEditionId, second.BookEditionId);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await context.Books.CountAsync(book => book.Title == title));
        Assert.Equal(1, await context.Authors.CountAsync(item => item.Name == author));
        Assert.Equal(1, await context.Publishers.CountAsync(item => item.Name == publisher));
        Assert.Equal(2, await context.BookEditions.CountAsync(edition => edition.BookId == first.BookId));
    }

    [Fact]
    public async Task Confirmed_isbn_can_be_added_to_a_library_using_the_existing_catalogue_edition()
    {
        const string isbn = "9780060850524";
        var authenticated = await IdentityAcceptanceTests.CreateAuthenticatedClientAsync(factory);
        using var client = authenticated.Client;
        var metadata = WebMetadata(isbn, "Library Import Acceptance Book", "Library Import Author");
        var imported = await PutDataAsync<BookMetadataLookupResult>(
            client,
            $"/api/book-editions/by-isbn/{isbn}",
            new ImportBookMetadataRequest { Metadata = metadata, ConfirmWebMetadata = true },
            HttpStatusCode.Created);
        var library = await PostDataAsync<LibraryViewModel>(client, "/api/libraries", new CreateLibraryRequest
        {
            Name = "ISBN Import Acceptance Library",
            Visibility = "Private"
        }, HttpStatusCode.Created);

        var copy = await PostDataAsync<LibraryBookViewModel>(
            client,
            $"/api/libraries/{library.Id}/book-imports/{isbn}",
            new ImportBookToLibraryRequest(),
            HttpStatusCode.Created);

        Assert.Equal(imported.BookEditionId, copy.BookEditionId);
        Assert.Equal(library.Id, copy.LibraryId);
    }

    private static BookMetadata WebMetadata(
        string isbn,
        string title,
        string author,
        string publisher = "Acceptance Press") =>
        new(
            title,
            null,
            "Reviewed metadata for acceptance testing.",
            "en",
            2020,
            [new BookMetadataAuthor(author)],
            new BookEditionMetadata(null, isbn, "Acceptance edition", publisher, "2020-01-01", 320, null),
            new BookMetadataProvenance(
                "OpenAIWebSearch",
                null,
                "https://example.com/provenance-book",
                true,
                [new BookMetadataSourceReference("https://example.com/provenance-book", "Verified source")]));

    private static async Task<T> GetDataAsync<T>(HttpClient client, string uri) where T : class
    {
        var response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        return await ReadDataAsync<T>(response);
    }

    private static async Task<T> PutDataAsync<T>(
        HttpClient client,
        string uri,
        object request,
        HttpStatusCode expected) where T : class
    {
        var response = await client.PutAsJsonAsync(uri, request);
        Assert.True(
            response.StatusCode == expected,
            $"Expected {(int)expected} but received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await ReadDataAsync<T>(response);
    }

    private static async Task<T> PostDataAsync<T>(
        HttpClient client,
        string uri,
        object request,
        HttpStatusCode expected) where T : class
    {
        var response = await client.PostAsJsonAsync(uri, request);
        Assert.True(
            response.StatusCode == expected,
            $"Expected {(int)expected} but received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await ReadDataAsync<T>(response);
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response) where T : class
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(payload?.Data);
        Assert.True(payload.Success);
        return payload.Data;
    }
}
