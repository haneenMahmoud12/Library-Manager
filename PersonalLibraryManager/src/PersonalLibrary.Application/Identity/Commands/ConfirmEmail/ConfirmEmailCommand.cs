namespace PersonalLibrary.Application.Identity.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Token);
