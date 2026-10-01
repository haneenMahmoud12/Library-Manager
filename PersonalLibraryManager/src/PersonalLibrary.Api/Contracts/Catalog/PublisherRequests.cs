namespace PersonalLibrary.Api.Contracts.Catalog;

public sealed class CreatePublisherRequest
{
    public required string Name { get; init; }
    public string? WebsiteUrl { get; init; }
}

public sealed class UpdatePublisherRequest
{
    public string? Name { get; init; }
    public string? WebsiteUrl { get; init; }
}
