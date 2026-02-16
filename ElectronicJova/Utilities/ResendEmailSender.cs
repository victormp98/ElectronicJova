using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Resend;

namespace ElectronicJova.Utilities
{
    // Update class to implement both interfaces
    public class ResendEmailSender : IEmailSender, Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public ResendEmailSender(IConfiguration configuration)
        {
            _apiKey = configuration["ResendSettings:ApiKey"];
            _senderEmail = configuration["ResendSettings:SenderEmail"];
            _senderName = configuration["ResendSettings:SenderName"];
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
