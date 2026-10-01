using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Application.Libraries.Services;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/libraries/{libraryId:guid}/books")]
public sealed class LibraryBooksController(ILibraryBookService libraryBookService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<LibraryBookViewModel>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LibraryBookViewModel>>> AddManually(
        Guid libraryId,
        AddBookManuallyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await libraryBookService.AddManuallyAsync(
            libraryId,
            new AddBookCopyDto(
                request.BookEditionId,
                request.LocationId,
                request.Format,
                request.ReadingStatus,
                request.Condition,
                request.AcquisitionDate,
                request.PurchasePrice,
                request.CurrencyCode,
                request.IsSigned,
                request.IsFavourite,
                request.PersonalNotes),
            cancellationToken);

        return Created(
            $"/api/libraries/{libraryId}/books/{result.Id}",
            ApiResponse.Succeeded(result, "Book added to library."));
    }

    [HttpGet]
    [HttpGet("GetLibraryContents")]
    [ProducesResponseType<ApiResponse<List<LibraryBookViewModel>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<LibraryBookViewModel>>>> GetContents(
        Guid libraryId,
        CancellationToken cancellationToken)
    {
        var result = await libraryBookService.GetContentsAsync(libraryId, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Library contents retrieved."));
    }
}
