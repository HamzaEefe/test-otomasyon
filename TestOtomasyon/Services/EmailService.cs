using System.Net;
using System.Net.Mail;

namespace TestOtomasyon.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("Email");

            var smtpHost = emailSettings["Host"];
            var smtpPort = int.Parse(emailSettings["Port"]!);
            var username = emailSettings["Username"];
            var password = emailSettings["Password"];
            var fromEmail = emailSettings["FromEmail"];
            var fromName = emailSettings["FromName"];

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password),
                Timeout = 10000
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
    }
}