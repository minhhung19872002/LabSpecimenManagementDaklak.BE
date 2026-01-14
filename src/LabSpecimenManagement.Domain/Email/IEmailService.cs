using System.Collections.Generic;
using System.Threading.Tasks;

namespace LabSpecimenManagement.Email;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendEmailAsync(IEnumerable<string> to, IEnumerable<string>? cc, string subject, string body, bool isHtml = true);
    Task SendEmailWithAttachmentAsync(IEnumerable<string> to, IEnumerable<string>? cc, string subject, string body, byte[] attachmentData, string attachmentFileName, bool isHtml = true);
    Task SendAccountCreatedEmailAsync(string to, string fullName, string username, string password);
    Task SendPasswordResetEmailAsync(string to, string fullName, string username, string newPassword);
}
