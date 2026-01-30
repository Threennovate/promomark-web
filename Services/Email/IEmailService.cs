namespace PromomaxWeb.Services.Email;

public interface IEmailService
{
    Task<string> RenderViewAsync(string viewName, object model);
    Task SendAsync(
        string to,
        string subject,
        string viewName,
        object model,
        string? replyTo = null,
        Dictionary<string, byte[]>? attachments = null,
        bool inlineAttachments = false,
        System.Net.Mail.MailAddress? fromOverride = null,
        IEnumerable<string>? bcc = null);
}
