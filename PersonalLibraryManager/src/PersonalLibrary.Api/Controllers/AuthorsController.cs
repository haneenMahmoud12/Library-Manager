using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Services;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/authors")]
public sealed class AuthorsController(IAuthorService authorService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<AuthorViewModel>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthorViewModel>>> Create(
        CreateAuthorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authorService.SaveAsync(
            new CreateAuthorDto(request.Name, request.Biography, request.BirthDate, request.DeathDate),
            cancellationToken);
        return Created($"/api/authors/{result.Id}", ApiResponse.Succeeded(result, "Author created."));
    }

    [HttpPatch("{authorId:guid}")]
    [ProducesResponseType<ApiResponse<AuthorViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AuthorViewModel>>> Update(
        Guid authorId,
        UpdateAuthorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authorService.SaveAsync(
            authorId,
            new UpdateAuthorDto(request.Name, request.Biography, request.BirthDate, request.DeathDate),
            cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Author updated."));
    }

    [HttpGet("{authorId:guid}")]
    [ProducesResponseType<ApiResponse<AuthorViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AuthorViewModel>>> GetById(
        Guid authorId,
        CancellationToken cancellationToken)
    {
        var result = await authorService.GetByIdAsync(authorId, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Author retrieved."));
    }

    [HttpGet("/api/books/{bookId:guid}/authors")]
    [ProducesResponseType<ApiResponse<List<AuthorViewModel>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<AuthorViewModel>>>> GetByBookId(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var result = await authorService.GetAllByBookIdAsync(bookId, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Book authors retrieved."));
    }

    [HttpGet("/api/books/authors")]
    [ProducesResponseType<ApiResponse<List<AuthorViewModel>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<AuthorViewModel>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await authorService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Authors retrieved."));
    }
}
