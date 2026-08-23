namespace PersonalLibrary.Infrastructure.Email;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Personal Library Manager";
    public string PublicApiBaseUrl { get; init; } = string.Empty;
}
