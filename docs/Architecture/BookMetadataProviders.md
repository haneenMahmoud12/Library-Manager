# Book metadata providers

`IBookMetadataProvider` is the application-facing ISBN lookup abstraction.
`FallbackBookMetadataProvider` implements it by trying every registered
`IBookMetadataSource` in ascending `Priority` order.

Current order:

1. Google Books (`Priority = 100`)
2. Open Library (`Priority = 200`), using `/isbn/{isbn}.json`
3. OpenAI web search (`Priority = 300`), using strict structured output

The lookup service always checks the local `BookEditions` table first. External
providers are contacted only when the normalized ISBN is not in the catalog.
`GET /api/books/lookup/{isbn}` returns a preview and never writes to the database.
AI/web-search previews include `requiresReview: true`, a warning, and source URLs.

After review, `POST /api/books/import/{isbn}` accepts the preview metadata and
stores the book, edition, authors, and publisher in one transaction. AI metadata
also requires `confirmWebMetadata: true`. Existing entities are reused by ISBN or
normalized catalog identity, and empty optional fields are enriched when the
provider supplies values.

## Adding another provider

1. Implement `IBookMetadataSource` in Infrastructure.
2. Give the source a unique `Name` and an appropriate `Priority`.
3. Normalize the provider response into `BookMetadata`; do not expose its native
   response model to Application or API projects.
4. Register its typed `HttpClient` and add the implementation as an
   `IBookMetadataSource` in `DependencyInjection`.

No changes to `FallbackBookMetadataProvider`, the lookup service, or the API
controller are required.
