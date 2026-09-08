using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Libraries;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Commands.CreateLibrary;
using PersonalLibrary.Application.Libraries.Commands.UpdateLibrary;
using PersonalLibrary.Application.Libraries.Queries.GetMyLibraries;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/libraries")]
public sealed class LibrariesController(
    CreateLibraryHandler createLibrary,
    UpdateLibraryHandler updateLibrary,
    GetMyLibrariesHandler getMyLibraries)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<CreateLibraryResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CreateLibraryResult>>> Create(
        CreateLibraryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createLibrary.HandleAsync(
            new CreateLibraryCommand(
                request.Name,
                request.Description,
                request.Visibility),
            cancellationToken);

        return Created(
            $"/api/libraries/{result.LibraryId}",
            ApiResponse.Succeeded(result, "Library created."));
    }

    [HttpPatch("{libraryId:guid}")]
    [ProducesResponseType<ApiResponse<UpdateLibraryResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UpdateLibraryResult>>> Update(
        Guid libraryId,
        UpdateLibraryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateLibrary.HandleAsync(
            new UpdateLibraryCommand(
                libraryId,
                request.Name,
                request.Description,
                request.Visibility),
            cancellationToken);

        return Ok(ApiResponse.Succeeded(result, "Library updated."));
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<GetMyLibrariesResult>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<GetMyLibrariesResult>>>> GetMyLibraries(
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
        var result = await getMyLibraries.HandleAsync(
            new GetMyLibrariesQuery(page),
            cancellationToken);

        return Ok(ApiResponse.Succeeded(result, "My libraries retrieved."));
    }
}
