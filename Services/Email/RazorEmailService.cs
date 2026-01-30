using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace PromomaxWeb.Services.Email;

public class RazorEmailService : IEmailService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly SmtpOptions _smtp;

    public RazorEmailService(
        IServiceProvider serviceProvider,
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IOptions<SmtpOptions> smtpOptions)
    {
        _serviceProvider = serviceProvider;
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _smtp = smtpOptions.Value;
    }

    public async Task<string> RenderViewAsync(string viewName, object model)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        await using var sw = new StringWriter();
        var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);
        if (viewResult.View == null)
        {
            throw new ArgumentNullException($"View '{viewName}' was not found. Ensure it exists under Views/ and that the name is correct.");
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
            sw,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }

    public async Task SendAsync(
        string to,
        string subject,
        string viewName,
        object model,
        string? replyTo = null,
        Dictionary<string, byte[]>? attachments = null,
        bool inlineAttachments = false,
        MailAddress? fromOverride = null,
        IEnumerable<string>? bcc = null)
    {
        var body = await RenderViewAsync(viewName, model);

        var fromAddress = fromOverride ?? (!string.IsNullOrWhiteSpace(_smtp.From) ? new MailAddress(_smtp.From) : null);
        if (fromAddress == null)
        {
            throw new InvalidOperationException("SMTP 'From' address is not configured. Set Umbraco:CMS:Global:Smtp:From in appsettings.json.");
        }

        using var mail = new MailMessage
        {
            From = fromAddress,
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mail.To.Add(new MailAddress(to));
        if (!string.IsNullOrEmpty(replyTo))
        {
            mail.ReplyToList.Add(new MailAddress(replyTo));
        }

        if (bcc != null)
        {
            foreach (var addr in bcc)
            {
                if (!string.IsNullOrWhiteSpace(addr))
                    mail.Bcc.Add(new MailAddress(addr));
            }
        }

        if (attachments != null && attachments.Count > 0)
        {
            var altView = AlternateView.CreateAlternateViewFromString(body, null, "text/html");
            foreach (var kvp in attachments)
            {
                var nameOrCid = kvp.Key;
                var bytes = kvp.Value;
                var stream = new MemoryStream(bytes);
                if (inlineAttachments)
                {
                    var linked = new LinkedResource(stream) { ContentId = nameOrCid };
                    altView.LinkedResources.Add(linked);
                }
                else
                {
                    mail.Attachments.Add(new Attachment(stream, nameOrCid));
                }
            }
            mail.AlternateViews.Add(altView);
        }

        using var client = new SmtpClient(_smtp.Host, _smtp.Port);
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        var secure = _smtp.SecureSocketOptions ?? "None";
        client.EnableSsl = !string.Equals(secure, "None", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(_smtp.Username))
        {
            client.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
        }
        else
        {
            client.UseDefaultCredentials = true;
        }

        await client.SendMailAsync(mail);
    }
}
