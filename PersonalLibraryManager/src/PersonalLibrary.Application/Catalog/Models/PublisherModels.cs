namespace PersonalLibrary.Application.Catalog.Models;

public sealed record CreatePublisherDto(string Name, string? WebsiteUrl);

public sealed record UpdatePublisherDto(string? Name, string? WebsiteUrl);

public sealed record PublisherViewModel(
    Guid Id,
    string Name,
    string? WebsiteUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
