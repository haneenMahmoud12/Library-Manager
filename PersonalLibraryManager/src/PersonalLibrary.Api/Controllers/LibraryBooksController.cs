using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Application.Libraries.Models;

namespace PersonalLibrary.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/libraries/{libraryId:guid}/books")]
    public class LibraryBooksController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<ApiResponse<LibraryViewModel>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
        public IActionResult AddManually() =>
            StatusCode(StatusCodes.Status501NotImplemented);

        [HttpGet("GetLibraryContents")]
        public IActionResult GetContents() =>
            StatusCode(StatusCodes.Status501NotImplemented);
    }
}
