using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Contracts.Libraries;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Application.Catalog.Services;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Application.Libraries.Services;

namespace PersonalLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BooksController(IBookService bookService) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<ApiResponse<BookViewModel>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<BookViewModel>>> Create(
        SaveBookRequest request,
        CancellationToken cancellationToken)
        {
            var result = await bookService.SaveAsync(
                new CreateBookDto(
                    request.Title,
                    request.OriginalTitle,
                    request.Description,
                    request.OriginalLanguageCode,
                    request.FirstPublishedYear,
                    request.Authors),
                cancellationToken);

            return Created(
                $"/api/books/{result.Id}",
                ApiResponse.Succeeded(result, "Book created."));
        }

        [HttpPatch("{bookId:guid}")]
        [ProducesResponseType<ApiResponse<BookViewModel>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<BookViewModel>>> Update(
            Guid bookId,
            SaveBookRequest request,
            CancellationToken cancellationToken)
        {
            var result = await bookService.SaveAsync(
                bookId,
                new UpdateBookDto(
                    request.Title,
                    request.OriginalTitle,
                    request.Description,
                    request.OriginalLanguageCode,
                    request.FirstPublishedYear,
                    request.Authors),
                cancellationToken);

            return Ok(ApiResponse.Succeeded(result, "Book updated."));
        }

        [HttpGet("{bookId:guid}")]
        [ProducesResponseType<ApiResponse<BookViewModel>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<BookViewModel>>> GetById(
        Guid bookId,
        CancellationToken cancellationToken)
        {
            var result = await bookService.GetByIdAsync(bookId, cancellationToken);
            return Ok(ApiResponse.Succeeded(result, "Book retrieved."));
        }
    }
}
