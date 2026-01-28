using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Email;

public class EmailService : IEmailService, ITransientDependency
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        await SendEmailAsync(new[] { to }, null, subject, body, isHtml);
    }

    public async Task SendEmailAsync(IEnumerable<string> to, IEnumerable<string>? cc, string subject, string body, bool isHtml = true)
    {
        await SendEmailWithAttachmentAsync(to, cc, subject, body, null, null, isHtml);
    }

    public async Task SendEmailWithAttachmentAsync(IEnumerable<string> to, IEnumerable<string>? cc, string subject, string body, byte[]? attachmentData, string? attachmentFileName, bool isHtml = true)
    {
        // Validate email settings
        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("Email settings chưa được cấu hình. Vui lòng kiểm tra cấu hình EmailSettings:FromEmail trong appsettings.json");
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            throw new InvalidOperationException("Email settings chưa được cấu hình. Vui lòng kiểm tra cấu hình EmailSettings:SmtpHost trong appsettings.json");
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                EnableSsl = _settings.EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            foreach (var toEmail in to)
            {
                if (!string.IsNullOrWhiteSpace(toEmail))
                {
                    mailMessage.To.Add(toEmail.Trim());
                }
            }

            if (cc != null)
            {
                foreach (var ccEmail in cc)
                {
                    if (!string.IsNullOrWhiteSpace(ccEmail))
                    {
                        mailMessage.CC.Add(ccEmail.Trim());
                    }
                }
            }

            // Add attachment if provided
            if (attachmentData != null && !string.IsNullOrEmpty(attachmentFileName))
            {
                var stream = new MemoryStream(attachmentData);
                var attachment = new Attachment(stream, attachmentFileName, "application/pdf");
                mailMessage.Attachments.Add(attachment);
            }

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Emails}", string.Join(", ", to));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Emails}", string.Join(", ", to));
            throw;
        }
    }

    public async Task SendAccountCreatedEmailAsync(string to, string fullName, string username, string password)
    {
        var subject = "Tài khoản Lab Specimen Management đã được tạo";
        var body = GetAccountCreatedEmailTemplate(fullName, username, password);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string to, string fullName, string username, string newPassword)
    {
        var subject = "Mật khẩu Lab Specimen Management đã được đặt lại";
        var body = GetPasswordResetEmailTemplate(fullName, username, newPassword);
        await SendEmailAsync(to, subject, body);
    }

    private string GetAccountCreatedEmailTemplate(string fullName, string username, string password)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #1890ff; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border: 1px solid #e8e8e8; }}
        .credentials {{ background-color: #fff; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #e8e8e8; }}
        .credential-item {{ margin: 10px 0; }}
        .credential-label {{ color: #666; font-size: 14px; }}
        .credential-value {{ font-size: 18px; font-weight: bold; color: #1890ff; font-family: monospace; }}
        .warning {{ background-color: #fff7e6; border: 1px solid #ffd591; padding: 15px; border-radius: 6px; margin-top: 20px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Chào mừng đến với Lab Specimen Management</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{fullName}</strong>,</p>
            <p>Tài khoản của bạn đã được tạo thành công trên hệ thống Lab Specimen Management.</p>

            <div class='credentials'>
                <h3>Thông tin đăng nhập:</h3>
                <div class='credential-item'>
                    <div class='credential-label'>Tên đăng nhập:</div>
                    <div class='credential-value'>{username}</div>
                </div>
                <div class='credential-item'>
                    <div class='credential-label'>Mật khẩu:</div>
                    <div class='credential-value'>{password}</div>
                </div>
            </div>

            <div class='warning'>
                <strong>Lưu ý quan trọng:</strong> Vui lòng đổi mật khẩu ngay sau khi đăng nhập lần đầu để đảm bảo an toàn cho tài khoản của bạn.
            </div>
        </div>
        <div class='footer'>
            <p>Email này được gửi tự động từ hệ thống Lab Specimen Management.</p>
            <p>Vui lòng không trả lời email này.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetPasswordResetEmailTemplate(string fullName, string username, string newPassword)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #fa8c16; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border: 1px solid #e8e8e8; }}
        .credentials {{ background-color: #fff; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #e8e8e8; }}
        .credential-item {{ margin: 10px 0; }}
        .credential-label {{ color: #666; font-size: 14px; }}
        .credential-value {{ font-size: 18px; font-weight: bold; color: #fa8c16; font-family: monospace; }}
        .warning {{ background-color: #fff7e6; border: 1px solid #ffd591; padding: 15px; border-radius: 6px; margin-top: 20px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Mật khẩu đã được đặt lại</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{fullName}</strong>,</p>
            <p>Mật khẩu tài khoản của bạn trên hệ thống Lab Specimen Management đã được đặt lại bởi quản trị viên.</p>

            <div class='credentials'>
                <h3>Thông tin đăng nhập mới:</h3>
                <div class='credential-item'>
                    <div class='credential-label'>Tên đăng nhập:</div>
                    <div class='credential-value'>{username}</div>
                </div>
                <div class='credential-item'>
                    <div class='credential-label'>Mật khẩu mới:</div>
                    <div class='credential-value'>{newPassword}</div>
                </div>
            </div>

            <div class='warning'>
                <strong>Lưu ý quan trọng:</strong> Vui lòng đổi mật khẩu ngay sau khi đăng nhập để đảm bảo an toàn cho tài khoản của bạn.
            </div>
        </div>
        <div class='footer'>
            <p>Email này được gửi tự động từ hệ thống Lab Specimen Management.</p>
            <p>Vui lòng không trả lời email này.</p>
        </div>
    </div>
</body>
</html>";
    }
}
