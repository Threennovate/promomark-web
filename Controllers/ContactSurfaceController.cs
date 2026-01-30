using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PromomaxWeb.Dtos;
using PromomaxWeb.Dtos.Emails;
using PromomaxWeb.Options;
using PromomaxWeb.Services.Email;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace PromomaxWeb.Controllers;

public class ContactSurfaceController(
    IUmbracoContextAccessor umbracoContextAccessor,
    IUmbracoDatabaseFactory databaseFactory,
    ServiceContext services,
    AppCaches appCaches,
    IProfilingLogger profilingLogger,
    IPublishedUrlProvider publishedUrlProvider,
    IEmailService emailService,
    IHttpClientFactory httpClientFactory,
    IOptions<GoogleRecaptchaOptions> recaptchaOptions)
    : SurfaceController(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
{
    private readonly IEmailService _emailService = emailService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly GoogleRecaptchaOptions _recaptchaOptions = recaptchaOptions.Value;

    private bool IsAjaxRequest() => string.Equals(Request?.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    private sealed class RecaptchaVerifyResponse
    {
        public bool success { get; set; }
        public double score { get; set; }
        public string? action { get; set; }
        public DateTimeOffset challenge_ts { get; set; }
        public string? hostname { get; set; }
        public string[]? error_codes { get; set; }
    }

    private async Task<bool> VerifyRecaptchaAsync(string? token, string? remoteIp)
    {
        if (string.IsNullOrWhiteSpace(_recaptchaOptions.SecretKey))
        {
            // If not configured, treat as passed to avoid blocking in dev.
            return true;
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var form = new Dictionary<string, string?>
            {
                ["secret"] = _recaptchaOptions.SecretKey,
                ["response"] = token,
                ["remoteip"] = remoteIp
            }!;
            using var content = new FormUrlEncodedContent(form!);
            using var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            // Accept if Google says success and score is reasonable for v3
            return result?.success == true && result.score >= 0.5;
        }
        catch
        {
            return false;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactFormDto postModel)
    {
        // Server-side reCAPTCHA v3 verification
        var recaptchaToken = Request?.Form["g-recaptcha-response"].ToString();
        var remoteIp = HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var recaptchaOk = await VerifyRecaptchaAsync(recaptchaToken, remoteIp);
        if (!recaptchaOk)
        {
            ModelState.AddModelError(string.Empty, "reCAPTCHA provjera nije uspjela. Pokušajte ponovno.");
        }

        if (string.IsNullOrWhiteSpace(postModel.Name))
            ModelState.AddModelError(nameof(postModel.Name), "Ime je obavezno.");
        if (string.IsNullOrWhiteSpace(postModel.Email) || !new EmailAddressAttribute().IsValid(postModel.Email))
            ModelState.AddModelError(nameof(postModel.Email), "Ispravan email je obavezan.");
        if (string.IsNullOrWhiteSpace(postModel.Message))
            ModelState.AddModelError(nameof(postModel.Message), "Poruka je obavezna.");

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return new JsonResult(new { success = false, alert = "alert-danger", message = "Molimo ispravite označene pogreške." });
            return CurrentUmbracoPage();
        }

        // Find HomePage to resolve recipient email
        var home = CurrentPage is IPublishedContent pc ? pc.AncestorOrSelf<HomePage>() : null;
        var to = home?.ContactEmail;
        if (string.IsNullOrWhiteSpace(to))
        {
            if (IsAjaxRequest())
                return new JsonResult(new { success = false, alert = "alert-danger", message = "Primatelj emaila nije postavljen." });
            ModelState.AddModelError("", "Primatelj emaila nije postavljen.");
            return CurrentUmbracoPage();
        }

        var model = new ContactEmailModel
        {
            Name = postModel.Name!.Trim(),
            Email = postModel.Email!.Trim(),
            Message = postModel.Message!.Trim(),
            PageName = CurrentPage?.Name,
            SubmittedAt = DateTimeOffset.Now
        };

        var subject = $"[Promomark] Nova poruka s kontaktnog obrasca";
        try
        {
            await _emailService.SendAsync(
                to: to,
                subject: subject,
                viewName: "Emails/ContactForm",
                model: model,
                replyTo: model.Email);
        }
        catch
        {
            if (IsAjaxRequest())
                return new JsonResult(new { success = false, alert = "alert-danger", message = "Slanje poruke nije uspjelo. Pokušajte ponovno kasnije." });
            ModelState.AddModelError("", "Slanje poruke nije uspjelo. Pokušajte ponovno kasnije.");
            return CurrentUmbracoPage();
        }

        if (IsAjaxRequest())
            return new JsonResult(new { success = true, alert = "alert-success", message = "Vaša poruka je poslana. Hvala!" });

        TempData["contactSuccess"] = "true";
        return RedirectToCurrentUmbracoPage();
    }
}
