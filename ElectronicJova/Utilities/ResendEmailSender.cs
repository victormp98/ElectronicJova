using ElectronicJova.Utilities;
using ResendClientLib = Resend; // Alias for the external library
using System.Threading.Tasks;

namespace ElectronicJova.Utilities
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly ResendClientLib.IResend _resendClient;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public ResendEmailSender(string apiKey, string senderEmail, string senderName)
        {
            _resendClient = ResendClientLib.ResendClient.Create(apiKey);
            _senderEmail = senderEmail;
            _senderName = senderName;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailOptions = new ResendClientLib.EmailMessage()
            {
                From = $"{_senderName} <{_senderEmail}>",
                To = email,
                Subject = subject,
                HtmlBody = htmlMessage
            };

            await _resendClient.EmailSendAsync(emailOptions);
        }
    }
}
