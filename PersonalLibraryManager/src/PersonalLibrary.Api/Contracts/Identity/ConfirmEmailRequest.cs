namespace PersonalLibrary.Api.Contracts.Identity;

public sealed class ConfirmEmailRequest
{
    public Guid UserId { get; init; }
    public required string Token { get; init; }
}
