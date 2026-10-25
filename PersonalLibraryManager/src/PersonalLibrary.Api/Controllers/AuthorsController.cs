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
[Route("api/authors")]
public sealed class AuthorsController(IAuthorService authorService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<AuthorViewModel>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthorViewModel>>> Create(
        CreateAuthorRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await authorService.SaveAsync(
            new CreateAuthorDto(request.Name, request.Biography, request.BirthDate, request.DeathDate),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { authorId = result.Id },
            ApiResponse.Succeeded(result, "Author created.", AuthorLinks(result.Id)));
    }

    [HttpPatch("{authorId:guid}")]
    [ProducesResponseType<ApiResponse<AuthorViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AuthorViewModel>>> Update(
        Guid authorId,
        UpdateAuthorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authorService.SaveAsync(
            authorId,
            new UpdateAuthorDto(request.Name, request.Biography, request.BirthDate, request.DeathDate),
            cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Author updated.", AuthorLinks(result.Id)));
    }

    [HttpGet("{authorId:guid}")]
    [ProducesResponseType<ApiResponse<AuthorViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AuthorViewModel>>> GetById(
        Guid authorId,
        CancellationToken cancellationToken)
    {
        var result = await authorService.GetByIdAsync(authorId, cancellationToken);
        if (HttpEntityTags.IsNotModified(HttpContext, result.Id, result.CreatedAt, result.UpdatedAt))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(ApiResponse.Succeeded(result, "Author retrieved.", AuthorLinks(result.Id)));
    }

    [HttpGet("/api/books/{bookId:guid}/authors")]
    [ProducesResponseType<ApiResponse<PagedResult<AuthorViewModel>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuthorViewModel>>>> GetByBookId(
        Guid bookId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await authorService.GetAllByBookIdAsync(bookId, cancellationToken);
        return Ok(ApiResponse.Succeeded(
            ApiPage.Create(result, pageNumber, pageSize),
            "Book authors retrieved."));
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<AuthorViewModel>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuthorViewModel>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await authorService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse.Succeeded(
            ApiPage.Create(result, pageNumber, pageSize),
            "Authors retrieved."));
    }

    private static IReadOnlyDictionary<string, ApiLink> AuthorLinks(Guid authorId) =>
        ApiLinks.ForResource(
            $"/api/authors/{authorId}",
            ("books", $"/api/authors/{authorId}/books", HttpMethods.Get));
}
