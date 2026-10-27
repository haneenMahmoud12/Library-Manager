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
[Route("api/publishers")]
public sealed class PublishersController(IPublisherService publisherService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<PublisherViewModel>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PublisherViewModel>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? searchText = null,
        [FromQuery] string orderBy = "Id",
        [FromQuery] OrderDirection orderDirection = OrderDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        var page = new PageRequest(pageNumber, pageSize, searchText, orderBy, orderDirection);
        var result = await publisherService.GetAllAsync(page, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Publishers retrieved."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PublisherViewModel>>> Create(
        CreatePublisherRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await publisherService.SaveAsync(
            new CreatePublisherDto(request.Name, request.WebsiteUrl), cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { publisherId = result.Id },
            ApiResponse.Succeeded(result, "Publisher created.", PublisherLinks(result.Id)));
    }

    [HttpPatch("{publisherId:guid}")]
    public async Task<ActionResult<ApiResponse<PublisherViewModel>>> Update(
        Guid publisherId,
        UpdatePublisherRequest request,
        CancellationToken cancellationToken)
    {
        var result = await publisherService.SaveAsync(
            publisherId, new UpdatePublisherDto(request.Name, request.WebsiteUrl), cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Publisher updated.", PublisherLinks(result.Id)));
    }

    [HttpGet("{publisherId:guid}")]
    public async Task<ActionResult<ApiResponse<PublisherViewModel>>> GetById(
        Guid publisherId,
        CancellationToken cancellationToken)
    {
        var result = await publisherService.GetByIdAsync(publisherId, cancellationToken);
        if (HttpEntityTags.IsNotModified(HttpContext, result.Id, result.CreatedAt, result.UpdatedAt))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(ApiResponse.Succeeded(result, "Publisher retrieved.", PublisherLinks(result.Id)));
    }

    [HttpGet("/api/books/{bookId:guid}/publishers")]
    public async Task<ActionResult<ApiResponse<PagedResult<PublisherViewModel>>>> GetByBookId(
        Guid bookId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? searchText = null,
        [FromQuery] string orderBy = "Id",
        [FromQuery] OrderDirection orderDirection = OrderDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        var result = await publisherService.GetAllByBookIdAsync(bookId, cancellationToken);
        return Ok(ApiResponse.Succeeded(
            ApiPage.Create(result, new PageRequest(pageNumber, pageSize, searchText, orderBy, orderDirection)),
            "Book publishers retrieved."));
    }

    private static IReadOnlyDictionary<string, ApiLink> PublisherLinks(Guid publisherId) =>
        ApiLinks.Self($"/api/publishers/{publisherId}");
}
