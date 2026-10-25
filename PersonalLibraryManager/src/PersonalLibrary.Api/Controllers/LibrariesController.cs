using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Caching;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Libraries;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Application.Libraries.Services;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/libraries")]
public sealed class LibrariesController(ILibraryService libraryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<LibraryViewModel>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LibraryViewModel>>> Create(
        CreateLibraryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await libraryService.SaveAsync(
            new CreateLibraryDto(
                request.Name,
                request.Description,
                request.Visibility),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { libraryId = result.Id },
            ApiResponse.Succeeded(
                result,
                "Library created.",
                LibraryLinks(result.Id)));
    }

    [HttpPatch("{libraryId:guid}")]
    [ProducesResponseType<ApiResponse<LibraryViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LibraryViewModel>>> Update(
        Guid libraryId,
        UpdateLibraryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await libraryService.SaveAsync(
            libraryId,
            new UpdateLibraryDto(
                request.Name,
                request.Description,
                request.Visibility),
            cancellationToken);

        return Ok(ApiResponse.Succeeded(result, "Library updated.", LibraryLinks(result.Id)));
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<LibraryListItemViewModel>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<LibraryListItemViewModel>>>> GetMyLibraries(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? searchText = null,
        [FromQuery] string orderBy = "Id",
        [FromQuery] OrderDirection orderDirection = OrderDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        var page = new PageRequest(
            pageNumber,
            pageSize,
            searchText,
            orderBy,
            orderDirection);
        var result = await libraryService.GetMyLibrariesAsync(page, cancellationToken);

        return Ok(ApiResponse.Succeeded(result, "My libraries retrieved."));
    }

    [HttpGet("{libraryId:guid}")]
    [ProducesResponseType<ApiResponse<LibraryViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LibraryViewModel>>> GetById(
        Guid libraryId,
        CancellationToken cancellationToken)
    {
        var result = await libraryService.GetByIdAsync(libraryId, cancellationToken);
        if (HttpEntityTags.IsNotModified(HttpContext, result.Id, result.CreatedAt, result.UpdatedAt))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(ApiResponse.Succeeded(result, "Library retrieved.", LibraryLinks(result.Id)));
    }

    private static IReadOnlyDictionary<string, ApiLink> LibraryLinks(Guid libraryId) =>
        ApiLinks.ForResource(
            $"/api/libraries/{libraryId}",
            ("books", $"/api/libraries/{libraryId}/books", HttpMethods.Get),
            ("addBook", $"/api/libraries/{libraryId}/books", HttpMethods.Post),
            ("importBook", $"/api/libraries/{libraryId}/book-imports/{{isbn}}", HttpMethods.Post));
}
