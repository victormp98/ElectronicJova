using System.Threading.Tasks;

namespace ElectronicJova.Utilities
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
