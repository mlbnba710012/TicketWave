using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace TicketWave.Web.Services
{
    public interface IEmailService
    {
        Task SendContactNotificationAsync(string senderName, string senderEmail,
            string subject, string category, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendContactNotificationAsync(string senderName, string senderEmail,
            string subject, string category, string message)
        {
            try
            {
                var smtpHost     = _config["EmailSettings:SmtpHost"];
                var smtpPort     = int.Parse(_config["EmailSettings:SmtpPort"]);
                var senderAddr   = _config["EmailSettings:SenderEmail"];
                var senderPwd    = _config["EmailSettings:SenderPassword"];
                var adminEmail   = _config["EmailSettings:AdminEmail"];

                var body = $@"
                    <h2>TicketWave 聯絡我們 - 新留言通知</h2>
                    <hr/>
                    <p><strong>姓名：</strong>{senderName}</p>
                    <p><strong>Email：</strong>{senderEmail}</p>
                    <p><strong>問題類別：</strong>{category}</p>
                    <p><strong>主旨：</strong>{subject}</p>
                    <p><strong>內容：</strong></p>
                    <p style='background:#f8f9fa;padding:15px;border-radius:5px;'>{message}</p>
                    <hr/>
                    <p style='color:#6c757d;font-size:12px;'>請至後台管理系統查看並回覆此留言。</p>";

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(senderAddr, senderPwd)
                };

                var mail = new MailMessage
                {
                    From       = new MailAddress(senderAddr, "TicketWave System"),
                    Subject    = $"[TicketWave 聯絡我們] {category} - {subject}",
                    Body       = body,
                    IsBodyHtml = true
                };
                mail.To.Add(adminEmail);

                await client.SendMailAsync(mail);
                _logger.LogInformation("聯絡通知信已寄送：{Email}", senderEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "寄送聯絡通知信失敗");
            }
        }
    }
}
