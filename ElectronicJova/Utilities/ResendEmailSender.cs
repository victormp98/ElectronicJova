using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Resend;

namespace ElectronicJova.Utilities
{
    // Update class to implement both the custom IEmailSender and the Identity IEmailSender
    public class ResendEmailSender : IEmailSender, Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public ResendEmailSender(IConfiguration configuration)
        {
            _apiKey = configuration["ResendSettings:ApiKey"] ?? throw new InvalidOperationException("Resend API key is not configured.");
            _senderEmail = configuration["ResendSettings:SenderEmail"] ?? throw new InvalidOperationException("Resend sender email is not configured.");
            _senderName = configuration["ResendSettings:SenderName"] ?? throw new InvalidOperationException("Resend sender name is not configured.");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new ResendClient(_apiKey);

            var message = new EmailMessage
            {
                From = $"{_senderName} <{_senderEmail}>",
                To = email,
                Subject = subject,
                HtmlBody = htmlMessage
            };

            await client.Email.SendAsync(message);
        }
    }
}
