using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SecureChat.Models;

namespace SecureChat.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode, string purpose)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"SecureChat - Mã OTP {purpose}";

            var body = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;padding:20px;background:#f6f8fb;'>
                    <div style='max-width:600px;margin:auto;background:#ffffff;border-radius:12px;padding:24px;border:1px solid #e5e7eb;'>
                        <h2 style='margin:0 0 16px;color:#111827;'>SecureChat</h2>
                        <p>Xin chào <strong>{fullName}</strong>,</p>
                        <p>Mã OTP cho thao tác <strong>{purpose}</strong> của bạn là:</p>
                        <div style='margin:20px 0;padding:18px;text-align:center;background:#111827;color:#ffffff;
                                    font-size:32px;font-weight:700;letter-spacing:6px;border-radius:12px;'>
                            {otpCode}
                        </div>
                        <p>Mã OTP có hiệu lực trong <strong>5 phút</strong>.</p>
                        <p>Nếu bạn không thực hiện thao tác này, hãy bỏ qua email.</p>
                    </div>
                </div>";

            message.Body = new BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}