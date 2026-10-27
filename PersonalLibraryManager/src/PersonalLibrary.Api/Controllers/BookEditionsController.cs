using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Caching;
using PersonalLibrary.Api.Pagination;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Services;
using PersonalLibrary.Application.Common.Pagination;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/book-editions")]
public sealed class BookEditionsController(IBookEditionService editionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<BookEditionViewModel>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookEditionViewModel>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? searchText = null,
        [FromQuery] string orderBy = "Id",
        [FromQuery] OrderDirection orderDirection = OrderDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        var page = new PageRequest(pageNumber, pageSize, searchText, orderBy, orderDirection);
        var result = await editionService.GetAllAsync(page, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Book editions retrieved."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookEditionViewModel>>> Create(
        CreateBookEditionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await editionService.SaveAsync(ToDto(request), cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { editionId = result.Id },
            ApiResponse.Succeeded(result, "Book edition created.", EditionLinks(result)));
    }

    [HttpPatch("{editionId:guid}")]
    public async Task<ActionResult<ApiResponse<BookEditionViewModel>>> Update(
        Guid editionId,
        UpdateBookEditionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await editionService.SaveAsync(editionId, ToDto(request), cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Book edition updated.", EditionLinks(result)));
    }

    [HttpGet("{editionId:guid}")]
    public async Task<ActionResult<ApiResponse<BookEditionViewModel>>> GetById(
        Guid editionId,
        CancellationToken cancellationToken)
    {
        var result = await editionService.GetByIdAsync(editionId, cancellationToken);
        if (HttpEntityTags.IsNotModified(HttpContext, result.Id, result.CreatedAt, result.UpdatedAt))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(ApiResponse.Succeeded(result, "Book edition retrieved.", EditionLinks(result)));
    }

    [HttpGet("/api/books/{bookId:guid}/editions")]
    public async Task<ActionResult<ApiResponse<PagedResult<BookEditionViewModel>>>> GetByBookId(
        Guid bookId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? searchText = null,
        [FromQuery] string orderBy = "Id",
        [FromQuery] OrderDirection orderDirection = OrderDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        var result = await editionService.GetAllByBookIdAsync(bookId, cancellationToken);
        return Ok(ApiResponse.Succeeded(
            ApiPage.Create(result, new PageRequest(pageNumber, pageSize, searchText, orderBy, orderDirection)),
            "Book editions retrieved."));
    }

    private static IReadOnlyDictionary<string, ApiLink> EditionLinks(BookEditionViewModel edition) =>
        ApiLinks.ForResource(
            $"/api/book-editions/{edition.Id}",
            ("book", $"/api/books/{edition.BookId}", HttpMethods.Get));

    private static CreateBookEditionDto ToDto(CreateBookEditionRequest request) =>
        new(request.BookId, request.PublisherId, request.Isbn10, request.Isbn13,
            request.EditionName, request.LanguageCode, request.PublicationDate, request.PageCount,
            request.CoverImageUrl, request.Description);

    private static UpdateBookEditionDto ToDto(UpdateBookEditionRequest request) =>
        new(request.BookId, request.PublisherId, request.Isbn10, request.Isbn13,
            request.EditionName, request.LanguageCode, request.PublicationDate, request.PageCount,
            request.CoverImageUrl, request.Description);
}
