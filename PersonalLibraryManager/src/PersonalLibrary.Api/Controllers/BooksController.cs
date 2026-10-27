using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Caching;
using PersonalLibrary.Api.Pagination;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Application.Catalog.Services;
using PersonalLibrary.Application.Common.Pagination;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/books")]
public sealed class BooksController(
    IBookService bookService,
    IBookMetadataLookupService metadataLookupService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<BookViewModel>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookViewModel>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? searchText = null,
        [FromQuery] string orderBy = "Id",
        [FromQuery] OrderDirection orderDirection = OrderDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        var page = new PageRequest(pageNumber, pageSize, searchText, orderBy, orderDirection);
        var result = await bookService.GetAllAsync(page, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Books retrieved."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookViewModel>>> Create(
        SaveBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await bookService.SaveAsync(
            new CreateBookDto(
                request.Title, request.OriginalTitle, request.Description,
                request.OriginalLanguageCode, request.FirstPublishedYear,
                request.Authors.Select(author => new BookAuthorDto(author.AuthorId, author.AuthorOrder)).ToList()),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { bookId = result.Id },
            ApiResponse.Succeeded(
                result,
                "Book created.",
                ApiLinks.ForResource(
                    $"/api/books/{result.Id}",
                    ("editions", $"/api/books/{result.Id}/editions", HttpMethods.Get))));
    }

    [HttpPut("{bookId:guid}")]
    public async Task<ActionResult<ApiResponse<BookViewModel>>> Update(
        Guid bookId,
        SaveBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookService.SaveAsync(
            bookId,
            new UpdateBookDto(
                request.Title, request.OriginalTitle, request.Description,
                request.OriginalLanguageCode, request.FirstPublishedYear,
                request.Authors.Select(author => new BookAuthorDto(author.AuthorId, author.AuthorOrder)).ToList()),
            cancellationToken);
        return Ok(ApiResponse.Succeeded(
            result,
            "Book updated.",
            ApiLinks.Self($"/api/books/{result.Id}")));
    }

    [HttpGet("{bookId:guid}")]
    public async Task<ActionResult<ApiResponse<BookViewModel>>> GetById(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var result = await bookService.GetByIdAsync(bookId, cancellationToken);
        if (HttpEntityTags.IsNotModified(HttpContext, result.Id, result.CreatedAt, result.UpdatedAt))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(ApiResponse.Succeeded(
            result,
            "Book retrieved.",
            ApiLinks.ForResource(
                $"/api/books/{result.Id}",
                ("editions", $"/api/books/{result.Id}/editions", HttpMethods.Get),
                ("authors", $"/api/books/{result.Id}/authors", HttpMethods.Get))));
    }

    [HttpGet("/api/book-metadata/{isbn}")]
    [ProducesResponseType<ApiResponse<BookMetadataLookupResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<BookMetadataLookupResult>>> LookupByIsbn(
        string isbn,
        CancellationToken cancellationToken)
    {
        var result = await metadataLookupService.PreviewByIsbnAsync(isbn, cancellationToken);
        return Ok(ApiResponse.Succeeded(
            result,
            result.FoundInLocalCatalog
                ? "The book edition already exists in the catalog."
                : result.RequiresReview
                    ? "⚠ Metadata found from web sources. Please review before adding."
                    : "Book metadata found. Please review before adding."));
    }

    [HttpPut("/api/book-editions/by-isbn/{isbn}")]
    [ProducesResponseType<ApiResponse<BookMetadataLookupResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BookMetadataLookupResult>>> ImportByIsbn(
        string isbn,
        ImportBookMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var result = await metadataLookupService.ImportAsync(
            isbn,
            request.Metadata,
            request.ConfirmWebMetadata,
            cancellationToken);
        var response = ApiResponse.Succeeded(
            result,
            result.FoundInLocalCatalog
                ? "The book edition already exists in the catalog."
                : "Book metadata imported into the catalog.");

        return result.FoundInLocalCatalog
            ? Ok(response)
            : CreatedAtAction(
                nameof(BookEditionsController.GetById),
                "BookEditions",
                new { editionId = result.BookEditionId },
                response);
    }

    [HttpGet("/api/authors/{authorId:guid}/books")]
    public async Task<ActionResult<ApiResponse<PagedResult<BookViewModel>>>> GetByAuthorId(
        Guid authorId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? searchText = null,
        [FromQuery] string orderBy = "Id",
        [FromQuery] OrderDirection orderDirection = OrderDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        var result = await bookService.GetAllByAuthorIdAsync(authorId, cancellationToken);
        return Ok(ApiResponse.Succeeded(
            ApiPage.Create(result, new PageRequest(pageNumber, pageSize, searchText, orderBy, orderDirection)),
            "Author books retrieved."));
    }
}
