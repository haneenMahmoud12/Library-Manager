using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Services;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/editions")]
public sealed class BookEditionsController(IBookEditionService editionService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookEditionViewModel>>> Create(
        CreateBookEditionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await editionService.SaveAsync(ToDto(request), cancellationToken);
        return Created($"/api/editions/{result.Id}", ApiResponse.Succeeded(result, "Book edition created."));
    }

    [HttpPatch("{editionId:guid}")]
    public async Task<ActionResult<ApiResponse<BookEditionViewModel>>> Update(
        Guid editionId,
        UpdateBookEditionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await editionService.SaveAsync(editionId, ToDto(request), cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Book edition updated."));
    }

    [HttpGet("{editionId:guid}")]
    public async Task<ActionResult<ApiResponse<BookEditionViewModel>>> GetById(
        Guid editionId,
        CancellationToken cancellationToken)
    {
        var result = await editionService.GetByIdAsync(editionId, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Book edition retrieved."));
    }

    [HttpGet("/api/books/{bookId:guid}/editions")]
    public async Task<ActionResult<ApiResponse<List<BookEditionViewModel>>>> GetByBookId(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var result = await editionService.GetAllByBookIdAsync(bookId, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Book editions retrieved."));
    }

    private static CreateBookEditionDto ToDto(CreateBookEditionRequest request) =>
        new(request.BookId, request.PublisherId, request.Isbn10, request.Isbn13,
            request.EditionName, request.LanguageCode, request.PublicationDate, request.PageCount,
            request.CoverImageUrl, request.Description);

    private static UpdateBookEditionDto ToDto(UpdateBookEditionRequest request) =>
        new(request.BookId, request.PublisherId, request.Isbn10, request.Isbn13,
            request.EditionName, request.LanguageCode, request.PublicationDate, request.PageCount,
            request.CoverImageUrl, request.Description);
}
