using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Resend;
using System.Threading.Tasks;

namespace ElectronicJova.Utilities
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly ResendClient _client;
        private readonly string _senderEmail;
        private readonly ILogger<ResendEmailSender> _logger;

        public ResendEmailSender(ResendClient client, IConfiguration configuration, ILogger<ResendEmailSender> logger)
        {
            _client = client;
            _senderEmail = configuration["ResendSettings:SenderEmail"] ?? "onboarding@resend.dev";
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation("Attempting to send email to {Email} with subject {Subject}", email, subject);
            try 
            {
                var message = new EmailMessage();
                message.From = _senderEmail;
                message.To.Add(email);
                message.Subject = subject;
                message.HtmlBody = htmlMessage;

                await _client.EmailSendAsync(message);
                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                // We swallow the exception to strictly follow user request, preventing the app from crashing on email failure.
            }
        }
    }
}
