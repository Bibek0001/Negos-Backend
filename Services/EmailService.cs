using System.Net;
using System.Net.Mail;

namespace Diyalo.Api.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    public async Task SendAsync(string toEmail, string toName, string subject, string body)
    {
        var smtp = _config["Email:SmtpHost"];
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var user = _config["Email:Username"];
        var pass = _config["Email:Password"];
        var from = _config["Email:From"];

        using var client = new SmtpClient(smtp, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };

        var mail = new MailMessage
        {
            From = new MailAddress(from!, "Negos"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mail.To.Add(new MailAddress(toEmail, toName));

        await client.SendMailAsync(mail);
    }
}
