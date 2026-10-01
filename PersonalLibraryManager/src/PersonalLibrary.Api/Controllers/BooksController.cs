using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Application.Catalog.Services;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/books")]
public sealed class BooksController(
    IBookService bookService,
    IBookMetadataLookupService metadataLookupService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookViewModel>>> Create(
        SaveBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookService.SaveAsync(
            new CreateBookDto(
                request.Title, request.OriginalTitle, request.Description,
                request.OriginalLanguageCode, request.FirstPublishedYear,
                request.Authors.Select(author => new BookAuthorDto(author.AuthorId, author.AuthorOrder)).ToList()),
            cancellationToken);
        return Created($"/api/books/{result.Id}", ApiResponse.Succeeded(result, "Book created."));
    }

    [HttpPatch("{bookId:guid}")]
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
        return Ok(ApiResponse.Succeeded(result, "Book updated."));
    }

    [HttpGet("{bookId:guid}")]
    public async Task<ActionResult<ApiResponse<BookViewModel>>> GetById(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var result = await bookService.GetByIdAsync(bookId, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Book retrieved."));
    }

    [HttpPost("import/{isbn}")]
    [ProducesResponseType<ApiResponse<BookMetadataLookupResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<BookMetadataLookupResult>>> ImportByIsbn(
        string isbn,
        CancellationToken cancellationToken)
    {
        var result = await metadataLookupService.GetByIsbnAsync(isbn, cancellationToken);
        return Ok(ApiResponse.Succeeded(
            result,
            result.FoundInLocalCatalog
                ? "The book edition already exists in the catalog."
                : "Book metadata imported into the catalog."));
    }

    [HttpGet("/api/authors/{authorId:guid}/books")]
    public async Task<ActionResult<ApiResponse<List<BookViewModel>>>> GetByAuthorId(
        Guid authorId,
        CancellationToken cancellationToken)
    {
        var result = await bookService.GetAllByAuthorIdAsync(authorId, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Author books retrieved."));
    }
}
