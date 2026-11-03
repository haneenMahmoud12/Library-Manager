using System.Net;
using System.Net.Http.Json;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Libraries;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Domain.Catalog.Enums;
using PersonalLibrary.IntegrationTests.Identity;

namespace PersonalLibrary.IntegrationTests;

public sealed class CatalogAndLibraryAcceptanceTests(RegistrationApiFactory factory)
    : IClassFixture<RegistrationApiFactory>
{
    [Fact]
    public async Task Authenticated_catalog_and_library_workflow_supports_create_update_relationships_and_multiple_copies()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/libraries")).StatusCode);

        var authenticated = await IdentityAcceptanceTests.CreateAuthenticatedClientAsync(factory);
        using var client = authenticated.Client;

        var invalidLibrary = await client.PostAsJsonAsync(
            "/api/libraries",
            new CreateLibraryRequest { Name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidLibrary.StatusCode);

        var library = await PostDataAsync<LibraryViewModel>(client, "/api/libraries", new CreateLibraryRequest
        {
            Name = "Acceptance Library",
            Description = "Initial description",
            Visibility = "Private"
        }, HttpStatusCode.Created);

        var updatedLibrary = await PatchDataAsync<LibraryViewModel>(client, $"/api/libraries/{library.Id}", new UpdateLibraryRequest
        {
            Name = "Updated Acceptance Library",
            Description = "Updated description",
            Visibility = "Public"
        });
        Assert.Equal("Updated Acceptance Library", updatedLibrary.Name);
        Assert.Equal("Updated description", updatedLibrary.Description);

        var author = await PostDataAsync<AuthorViewModel>(client, "/api/authors", new CreateAuthorRequest
        {
            Name = "Acceptance Author",
            Biography = "Initial biography"
        }, HttpStatusCode.Created);
        var updatedAuthor = await PatchDataAsync<AuthorViewModel>(client, $"/api/authors/{author.Id}", new UpdateAuthorRequest
        {
            Name = "Updated Acceptance Author",
            Biography = "Updated biography",
            BirthDate = new DateOnly(1980, 1, 2)
        });
        Assert.Equal("Updated Acceptance Author", updatedAuthor.Name);

        var publisher = await PostDataAsync<PublisherViewModel>(client, "/api/publishers", new CreatePublisherRequest
        {
            Name = "Acceptance Press",
            WebsiteUrl = "https://example.com"
        }, HttpStatusCode.Created);
        var updatedPublisher = await PatchDataAsync<PublisherViewModel>(client, $"/api/publishers/{publisher.Id}", new UpdatePublisherRequest
        {
            Name = "Updated Acceptance Press",
            WebsiteUrl = "https://example.org"
        });
        Assert.Equal("https://example.org", updatedPublisher.WebsiteUrl);

        var book = await PostDataAsync<BookViewModel>(client, "/api/books", new SaveBookRequest
        {
            Title = "Acceptance Book",
            Description = "Initial book description",
            Authors = [new BookAuthorRequest { AuthorId = author.Id, AuthorOrder = 1 }]
        }, HttpStatusCode.Created);
        var updatedBook = await PutDataAsync<BookViewModel>(client, $"/api/books/{book.Id}", new SaveBookRequest
        {
            Title = "Updated Acceptance Book",
            Description = "Updated book description",
            Authors = [new BookAuthorRequest { AuthorId = author.Id, AuthorOrder = 1 }]
        });
        Assert.Equal("Updated Acceptance Book", updatedBook.Title);
        Assert.Single(updatedBook.Authors);

        var ownedEdition = await PostDataAsync<BookEditionViewModel>(client, "/api/book-editions", new CreateBookEditionRequest
        {
            BookId = book.Id,
            PublisherId = publisher.Id,
            Isbn13 = "9780306406157",
            EditionName = "First acceptance edition",
            LanguageCode = "en",
            PageCount = 240
        }, HttpStatusCode.Created);
        var differentEdition = await PostDataAsync<BookEditionViewModel>(client, "/api/book-editions", new CreateBookEditionRequest
        {
            BookId = book.Id,
            PublisherId = publisher.Id,
            Isbn13 = "9780140328721",
            EditionName = "Different acceptance edition"
        }, HttpStatusCode.Created);
        var updatedEdition = await PatchDataAsync<BookEditionViewModel>(client, $"/api/book-editions/{ownedEdition.Id}", new UpdateBookEditionRequest
        {
            EditionName = "Updated first edition",
            PageCount = 250,
            Description = "Updated edition description"
        });
        Assert.Equal(250, updatedEdition.PageCount);

        var firstCopy = await AddCopyAsync(client, library.Id, ownedEdition.Id);
        var secondCopy = await AddCopyAsync(client, library.Id, ownedEdition.Id);
        Assert.NotEqual(firstCopy.Id, secondCopy.Id);

        var contents = await GetDataAsync<PagedResult<LibraryBookViewModel>>(
            client,
            $"/api/libraries/{library.Id}/books");
        Assert.Equal(2, contents.TotalCount);
        Assert.All(contents.Items, copy => Assert.Equal(library.Id, copy.LibraryId));

        var exact = await GetDataAsync<BookOwnershipResult>(
            client,
            $"/api/libraries/{library.Id}/books/ownership/9780306406157");
        Assert.True(exact.IsOwned);
        Assert.Equal(2, exact.CopyCount);

        var different = await GetDataAsync<BookOwnershipResult>(
            client,
            $"/api/libraries/{library.Id}/books/ownership/9780140328721");
        Assert.False(different.IsOwned);
        Assert.True(different.OwnsDifferentEdition);
        Assert.Equal("DifferentEditionOwned", different.OwnershipStatus);

        var unownedBook = await PostDataAsync<BookViewModel>(client, "/api/books", new SaveBookRequest
        {
            Title = "Unowned Acceptance Book",
            Authors = [new BookAuthorRequest { AuthorId = author.Id, AuthorOrder = 1 }]
        }, HttpStatusCode.Created);
        _ = await PostDataAsync<BookEditionViewModel>(client, "/api/book-editions", new CreateBookEditionRequest
        {
            BookId = unownedBook.Id,
            Isbn13 = "9780061120084",
            EditionName = "Unowned edition"
        }, HttpStatusCode.Created);
        var notOwned = await GetDataAsync<BookOwnershipResult>(
            client,
            $"/api/libraries/{library.Id}/books/ownership/9780061120084");
        Assert.False(notOwned.IsBookOwned);
        Assert.Equal("NotOwned", notOwned.OwnershipStatus);

        var metadataPreview = await GetDataAsync<BookMetadataLookupResult>(
            client,
            "/api/book-metadata/9780306406157");
        Assert.True(metadataPreview.FoundInLocalCatalog);
        Assert.Equal("LocalCatalog", metadataPreview.Metadata.Provenance.Provider);

        var catalogImport = await PutDataAsync<BookMetadataLookupResult>(
            client,
            "/api/book-editions/by-isbn/9780306406157",
            new ImportBookMetadataRequest());
        Assert.True(catalogImport.FoundInLocalCatalog);
        Assert.Equal(ownedEdition.Id, catalogImport.BookEditionId);

        var authors = await GetDataAsync<PagedResult<AuthorViewModel>>(client, $"/api/books/{book.Id}/authors");
        var editions = await GetDataAsync<PagedResult<BookEditionViewModel>>(client, $"/api/books/{book.Id}/editions");
        var books = await GetDataAsync<PagedResult<BookViewModel>>(client, $"/api/authors/{author.Id}/books");
        var publishers = await GetDataAsync<PagedResult<PublisherViewModel>>(client, $"/api/books/{book.Id}/publishers");
        Assert.Contains(authors.Items, item => item.Id == author.Id);
        Assert.Equal(2, editions.TotalCount);
        Assert.Contains(books.Items, item => item.Id == book.Id);
        Assert.Contains(publishers.Items, item => item.Id == publisher.Id);

        var listedLibraries = await GetDataAsync<PagedResult<LibraryListItemViewModel>>(
            client,
            "/api/libraries?searchText=Updated&orderBy=Name&orderDirection=Ascending");
        Assert.Contains(listedLibraries.Items, item => item.Id == library.Id);

        var otherUser = await IdentityAcceptanceTests.CreateAuthenticatedClientAsync(factory);
        using var otherClient = otherUser.Client;
        Assert.Equal(HttpStatusCode.Forbidden, (await otherClient.GetAsync($"/api/libraries/{library.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherClient.GetAsync($"/api/libraries/{library.Id}/books")).StatusCode);

        _ = differentEdition;
    }

    [Fact]
    public async Task Catalog_validation_and_missing_resources_return_failure_responses()
    {
        var authenticated = await IdentityAcceptanceTests.CreateAuthenticatedClientAsync(factory);
        using var client = authenticated.Client;

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(
            "/api/authors", new CreateAuthorRequest { Name = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(
            "/api/publishers", new CreatePublisherRequest { Name = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(
            "/api/books", new SaveBookRequest { Title = "", Authors = [] })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/books/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(
            "/api/book-metadata/not-an-isbn")).StatusCode);
    }

    private static Task<LibraryBookViewModel> AddCopyAsync(HttpClient client, Guid libraryId, Guid editionId) =>
        PostDataAsync<LibraryBookViewModel>(client, $"/api/libraries/{libraryId}/books", new AddBookManuallyRequest
        {
            BookEditionId = editionId,
            Format = BookFormat.Paperback,
            ReadingStatus = ReadingStatus.Unread,
            PersonalNotes = "Acceptance copy"
        }, HttpStatusCode.Created);

    private static async Task<T> GetDataAsync<T>(HttpClient client, string uri) where T : class
    {
        var response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        return await ReadDataAsync<T>(response);
    }

    private static async Task<T> PostDataAsync<T>(HttpClient client, string uri, object request, HttpStatusCode status) where T : class
    {
        var response = await client.PostAsJsonAsync(uri, request);
        Assert.True(
            response.StatusCode == status,
            $"Expected {(int)status} but received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await ReadDataAsync<T>(response);
    }

    private static async Task<T> PatchDataAsync<T>(HttpClient client, string uri, object request) where T : class
    {
        var response = await client.PatchAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return await ReadDataAsync<T>(response);
    }

    private static async Task<T> PutDataAsync<T>(HttpClient client, string uri, object request) where T : class
    {
        var response = await client.PutAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
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
