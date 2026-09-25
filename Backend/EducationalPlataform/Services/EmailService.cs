using EducationalPlataform.Interface;
using System.Net;
using System.Net.Mail;

namespace EducationalPlataform.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var smtpHost = _configuration["Smtp:Host"];
            var smtpUser = _configuration["Smtp:User"];
            var smtpPass = _configuration["Smtp:Pass"];
            var fromEmail = _configuration["Smtp:From"];

            if (string.IsNullOrWhiteSpace(smtpHost)
                || string.IsNullOrWhiteSpace(smtpUser)
                || string.IsNullOrWhiteSpace(smtpPass)
                || smtpUser.Contains("seuemail", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var mailMessage = new MailMessage(fromEmail ?? smtpUser, to, subject, body)
            {
                IsBodyHtml = false
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}
