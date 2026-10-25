using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Caching;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Libraries;
using PersonalLibrary.Api.Pagination;
using PersonalLibrary.Application.Common.Pagination;
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
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LibraryBookViewModel>>> AddManually(
        Guid libraryId,
        AddBookManuallyRequest request,
        CancellationToken cancellationToken = default)
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

        return CreatedAtAction(
            nameof(GetById),
            new { libraryId, bookCopyId = result.Id },
            ApiResponse.Succeeded(result, "Book added to library.", BookCopyLinks(result)));
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<LibraryBookViewModel>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<LibraryBookViewModel>>>> GetContents(
        Guid libraryId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await libraryBookService.GetContentsAsync(libraryId, cancellationToken);
        return Ok(ApiResponse.Succeeded(
            ApiPage.Create(result, pageNumber, pageSize),
            "Library contents retrieved.",
            ApiLinks.Self($"/api/libraries/{libraryId}/books?pageNumber={pageNumber}&pageSize={pageSize}")));
    }

    [HttpGet("{bookCopyId:guid}")]
    [ProducesResponseType<ApiResponse<LibraryBookViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LibraryBookViewModel>>> GetById(
        Guid libraryId,
        Guid bookCopyId,
        CancellationToken cancellationToken)
    {
        var result = await libraryBookService.GetByIdAsync(
            libraryId,
            bookCopyId,
            cancellationToken);
        if (HttpEntityTags.IsNotModified(HttpContext, result.Id, result.CreatedAt, result.UpdatedAt))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(ApiResponse.Succeeded(result, "Library book retrieved.", BookCopyLinks(result)));
    }

    [HttpGet("ownership/{isbn}")]
    [ProducesResponseType<ApiResponse<BookOwnershipResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BookOwnershipResult>>> CheckOwnership(
        Guid libraryId,
        string isbn,
        CancellationToken cancellationToken)
    {
        var result = await libraryBookService.CheckOwnershipAsync(libraryId, isbn, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Ownership check completed."));
    }

    [HttpPost("/api/libraries/{libraryId:guid}/book-imports/{isbn}")]
    [ProducesResponseType<ApiResponse<LibraryBookViewModel>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LibraryBookViewModel>>> ImportByIsbn(
        Guid libraryId,
        string isbn,
        ImportBookToLibraryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await libraryBookService.ImportAsync(
            libraryId,
            isbn,
            new ImportBookToLibraryDto(
                request.Metadata,
                request.ConfirmWebMetadata,
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

        return CreatedAtAction(
            nameof(GetById),
            new { libraryId, bookCopyId = result.Id },
            ApiResponse.Succeeded(
                result,
                "Book imported and added to library.",
                BookCopyLinks(result)));
    }

    private static IReadOnlyDictionary<string, ApiLink> BookCopyLinks(LibraryBookViewModel copy) =>
        ApiLinks.ForResource(
            $"/api/libraries/{copy.LibraryId}/books/{copy.Id}",
            ("library", $"/api/libraries/{copy.LibraryId}", HttpMethods.Get),
            ("edition", $"/api/book-editions/{copy.BookEditionId}", HttpMethods.Get));
}
