using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Resend;
using System.Threading.Tasks;

namespace ElectronicJova.Utilities
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _senderEmail;

        public ResendEmailSender(IConfiguration configuration)
        {
            _apiKey = configuration["ResendSettings:ApiKey"] ?? throw new InvalidOperationException("Resend API key is not configured.");
            _senderEmail = configuration["ResendSettings:SenderEmail"] ?? throw new InvalidOperationException("Resend sender email is not configured.");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new ResendClient(_apiKey);

            var message = new EmailMessage()
            {
                From = _senderEmail,
                To = email,
                Subject = subject,
                HtmlBody = htmlMessage
            };

            await client.Email.SendAsync(message);
        }
    }
}
