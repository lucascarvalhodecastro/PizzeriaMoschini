using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace PizzeriaMoschini.Services
{
    public class EmailService : IEmailSender
    {
        // Configuration object to access app settings
        private readonly IConfiguration _configuration;

        // Constructor to initialize the configuration object
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Get email settings from configuration
            var emailSettings = _configuration.GetSection("EmailSettings");

            // Create a new email message
            var email = new MimeMessage();

            // Set the sender's address and name
            email.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));

            // Set the recipient's address
            email.To.Add(new MailboxAddress("", toEmail));

            // Set the subject of the email
            email.Subject = subject;

            // Create the email body with HTML content
            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(emailSettings["SmtpServer"], int.Parse(emailSettings["SmtpPort"]), MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(emailSettings["Username"], emailSettings["Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
