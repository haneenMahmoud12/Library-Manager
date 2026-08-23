using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;

namespace PersonalLibrary.Infrastructure.Email;

internal sealed class SmtpEmailService(IOptions<SmtpEmailOptions> options)
    : IEmailService
{
    public async Task SendConfirmationEmailAsync(
        EmailConfirmationChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        EnsureConfigured(settings);

        var confirmationUrl =
            $"{settings.PublicApiBaseUrl.TrimEnd('/')}/api/auth/confirm-email" +
            $"?userId={challenge.UserId:D}" +
            $"&token={Uri.EscapeDataString(challenge.Token)}";

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = "Confirm your Personal Library Manager account",
            Body = $"Hello {HtmlEncoder.Default.Encode(challenge.Name)},<br><br>" +
                   $"Please <a href=\"{HtmlEncoder.Default.Encode(confirmationUrl)}\">confirm your email address</a>.",
            IsBodyHtml = true
        };
        message.To.Add(challenge.Email);

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl,
            Credentials = new NetworkCredential(settings.Username, settings.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private static void EnsureConfigured(SmtpEmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host) ||
            string.IsNullOrWhiteSpace(options.FromAddress) ||
            string.IsNullOrWhiteSpace(options.PublicApiBaseUrl))
        {
            throw new InvalidOperationException(
                "Email:Host, Email:FromAddress, and Email:PublicApiBaseUrl must be configured.");
        }
    }
}
