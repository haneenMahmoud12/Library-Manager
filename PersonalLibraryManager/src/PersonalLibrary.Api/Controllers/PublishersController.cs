using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Api.Contracts.Catalog;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Services;

namespace PersonalLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/publishers")]
public sealed class PublishersController(IPublisherService publisherService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PublisherViewModel>>> Create(
        CreatePublisherRequest request,
        CancellationToken cancellationToken)
    {
        var result = await publisherService.SaveAsync(
            new CreatePublisherDto(request.Name, request.WebsiteUrl), cancellationToken);
        return Created($"/api/publishers/{result.Id}", ApiResponse.Succeeded(result, "Publisher created."));
    }

    [HttpPatch("{publisherId:guid}")]
    public async Task<ActionResult<ApiResponse<PublisherViewModel>>> Update(
        Guid publisherId,
        UpdatePublisherRequest request,
        CancellationToken cancellationToken)
    {
        var result = await publisherService.SaveAsync(
            publisherId, new UpdatePublisherDto(request.Name, request.WebsiteUrl), cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Publisher updated."));
    }

    [HttpGet("{publisherId:guid}")]
    public async Task<ActionResult<ApiResponse<PublisherViewModel>>> GetById(
        Guid publisherId,
        CancellationToken cancellationToken)
    {
        var result = await publisherService.GetByIdAsync(publisherId, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Publisher retrieved."));
    }

    [HttpGet("/api/books/{bookId:guid}/publishers")]
    public async Task<ActionResult<ApiResponse<List<PublisherViewModel>>>> GetByBookId(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var result = await publisherService.GetAllByBookIdAsync(bookId, cancellationToken);
        return Ok(ApiResponse.Succeeded(result, "Book publishers retrieved."));
    }
}
