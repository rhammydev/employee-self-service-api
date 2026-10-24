using EmployeeSelfServiceAPI.Domain.Settings;
using EmployeeSelfServiceAPI.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class SmtpEmailSender(IOptions<SmtpMail> options) : IEmailSender
{
    private readonly SmtpMail _settings = options.Value;

    public bool IsConfigured => _settings.IsConfigured;

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        var senderName = string.IsNullOrWhiteSpace(_settings.SenderName) ? "Employee Self-Service" : _settings.SenderName;
        var senderEmail = string.IsNullOrWhiteSpace(_settings.SenderEmail) ? _settings.Username : _settings.SenderEmail;

        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = body };   

        var socketOptions = _settings.Port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto
        };

        using var client = new SmtpClient { CheckCertificateRevocation = false };
        await client.ConnectAsync(_settings.Server, _settings.Port, socketOptions, cancellationToken);
        await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);   
        await client.DisconnectAsync(true, cancellationToken);
    }
}
