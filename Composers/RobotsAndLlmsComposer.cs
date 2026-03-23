using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

namespace PromomarkWeb.Composers;

public sealed class RobotsAndLlmsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(nameof(RobotsAndLlmsComposer))
            {
                PreRouting = app =>
                {
                    app.Use(async (context, next) =>
                    {
                        var path = context.Request.Path.Value ?? string.Empty;

                        if (path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase))
                        {
                            await WriteRobotsAsync(context);
                            return;
                        }

                        if (path.Equals("/llms.txt", StringComparison.OrdinalIgnoreCase))
                        {
                            await WriteLlmsAsync(context);
                            return;
                        }

                        await next();
                    });
                }
            });
        });
    }

    private static Task WriteRobotsAsync(HttpContext context)
    {
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        var body = string.Join("\n", new[]
        {
            "User-agent: *",
            "Disallow: /umbraco/",
            $"Sitemap: {baseUrl}/sitemap"
        }) + "\n";

        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.Headers.CacheControl = "public, max-age=86400";
        return context.Response.WriteAsync(body);
    }

    private static Task WriteLlmsAsync(HttpContext context)
    {
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        var lines = new List<string>
        {
            "# Promomark",
            "> B2B market access partner for premium brands entering Croatian retail.",
            "",
            "Promomark d.o.o. connects international and domestic brands with leading Croatian retail chains by providing key account management, distribution, and logistics. The website is B2B-focused; end-consumer traffic is directed to brand webshops."
        };

        var llmsByCulture = BuildLlmsContentByCulture(context, baseUrl);
        if (llmsByCulture.Count == 0)
        {
            llmsByCulture.Add(new LlmsCultureContent(null, new LlmsContent(
                new List<LlmLink>
                {
                    new("Home", $"{baseUrl}/", "Overview of Promomark and its market access services.")
                },
                new List<LlmLink>())));
        }

        foreach (var cultureContent in llmsByCulture)
        {
            lines.Add("");
            lines.Add(string.IsNullOrWhiteSpace(cultureContent.Culture)
                ? "## Key Pages"
                : $"## Key Pages ({cultureContent.Culture})");

            foreach (var link in cultureContent.Content.KeyPages)
            {
                lines.Add(FormatLink(link));
            }

            if (cultureContent.Content.Brands.Count > 0)
            {
                lines.Add("");
                lines.Add(string.IsNullOrWhiteSpace(cultureContent.Culture)
                    ? "## Brands"
                    : $"## Brands ({cultureContent.Culture})");

                foreach (var brand in cultureContent.Content.Brands)
                {
                    lines.Add(FormatLink(brand));
                }
            }
        }

        lines.Add("");
        lines.Add("## Sitemap");
        lines.Add($"- [XML sitemap]({baseUrl}/sitemap): Full list of public pages and language variants.");
        lines.Add("");
        lines.Add("## Optional");
        lines.Add($"- [Robots]({baseUrl}/robots.txt): Crawler access rules.");

        var body = string.Join("\n", lines) + "\n";

        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.Headers.CacheControl = "public, max-age=86400";
        return context.Response.WriteAsync(body);
    }

    private static List<LlmsCultureContent> BuildLlmsContentByCulture(HttpContext context, string baseUrl)
    {
        var contentQuery = context.RequestServices.GetRequiredService<IPublishedContentQuery>();
        var umbracoContextFactory = context.RequestServices.GetRequiredService<IUmbracoContextFactory>();
        var variationContextAccessor = context.RequestServices.GetRequiredService<IVariationContextAccessor>();

        using var cref = umbracoContextFactory.EnsureUmbracoContext();
        var root = contentQuery.ContentAtRoot().FirstOrDefault();
        if (root == null)
        {
            return new List<LlmsCultureContent>();
        }

        var cultures = GetPublishedCultures(root);
        var results = new List<LlmsCultureContent>();

        foreach (var culture in cultures)
        {
            var content = BuildLlmsContentForCulture(root, baseUrl, culture, variationContextAccessor);
            if (content.KeyPages.Count == 0 && content.Brands.Count == 0)
            {
                continue;
            }

            results.Add(new LlmsCultureContent(culture, content));
        }

        return results;
    }

    private static LlmsContent BuildLlmsContentForCulture(
        IPublishedContent root,
        string baseUrl,
        string? culture,
        IVariationContextAccessor variationContextAccessor)
    {
        var homePage = root as HomePage;
        var previousVariation = variationContextAccessor.VariationContext;
        try
        {
            variationContextAccessor.VariationContext = new VariationContext(culture);
            var allChildren = root.Children.Where(IsMainPage).ToList();
            var brandsPage = allChildren.OfType<Brands>().FirstOrDefault();
            var aboutPage = allChildren.OfType<AboutUs>().FirstOrDefault();
            var contactPage = allChildren.OfType<ContactPage>().FirstOrDefault();

            var keyPages = new List<LlmLink>();
            AddKeyPage(keyPages, homePage ?? root, baseUrl, culture,
                "Overview of Promomark’s market access model and strategic value.");
            AddKeyPage(keyPages, brandsPage, baseUrl, culture,
                "Portfolio of represented brands and categories.");
            AddKeyPage(keyPages, aboutPage, baseUrl, culture,
                "Company profile, history, and business segments.");
            AddKeyPage(keyPages, contactPage, baseUrl, culture,
                "Partner inquiries and department contacts.");

            var brandLinks = new List<LlmLink>();
            if (brandsPage != null)
            {
                var brandChildren = brandsPage.Descendants().Where(IsMainPage);
                foreach (var brand in brandChildren.OfType<Brand>())
                {
                    var url = GetAbsoluteUrl(brand, baseUrl, culture);
                    if (!IsValidUrl(url))
                    {
                        continue;
                    }

                    var title = GetTitle(brand, culture);
                    var description = GetDescription(brand, culture, brand.Subtitle);
                    brandLinks.Add(new LlmLink(title, url!, description));
                }
            }

            brandLinks = brandLinks
                .GroupBy(link => link.Url, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(link => link.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            keyPages = keyPages
                .GroupBy(link => link.Url, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            return new LlmsContent(keyPages, brandLinks);
        }
        finally
        {
            variationContextAccessor.VariationContext = previousVariation;
        }
    }

    private static List<string?> GetPublishedCultures(IPublishedContent content)
    {
        if (!content.ContentType.VariesByCulture())
        {
            return new List<string?> { null };
        }

        return content.Cultures
            .Where(x => content.IsPublished(x.Key))
            .Select(x => (string?)x.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddKeyPage(
        List<LlmLink> links,
        IPublishedContent? content,
        string baseUrl,
        string? culture,
        string fallbackDescription)
    {
        if (content == null || !IsMainPage(content))
        {
            return;
        }

        var url = GetAbsoluteUrl(content, baseUrl, culture);
        if (!IsValidUrl(url))
        {
            return;
        }

        var title = GetTitle(content, culture);
        var description = GetDescription(content, culture, fallbackDescription);
        links.Add(new LlmLink(title, url!, description));
    }

    private static string? GetAbsoluteUrl(IPublishedContent content, string baseUrl, string? culture)
    {
        var absolute = content.Url(culture: culture, mode: UrlMode.Absolute);
        if (!IsValidUrl(absolute))
        {
            var relative = content.Url(culture: culture);
            if (IsValidUrl(relative))
            {
                absolute = $"{baseUrl.TrimEnd('/')}{relative}";
            }
        }

        return absolute;
    }

    private static string GetTitle(IPublishedContent content, string? culture)
    {
        var title = content.Value<string>("pageTitle", culture: culture);
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        var name = content.Name(culture);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return content.Name;
    }

    private static string GetDescription(IPublishedContent content, string? culture, string fallback)
    {
        var meta = content.Value<string>("metaDescription", culture: culture);
        if (!string.IsNullOrWhiteSpace(meta))
        {
            return meta.Trim();
        }

        return fallback;
    }

    private static string? GetDefaultCulture(IPublishedContent content)
    {
        if (!content.ContentType.VariesByCulture())
        {
            return null;
        }

        var byUrlSegment = content.Cultures
            .FirstOrDefault(c => string.IsNullOrEmpty(c.Value.UrlSegment)).Key;
        if (!string.IsNullOrWhiteSpace(byUrlSegment))
        {
            return byUrlSegment;
        }

        var published = content.Cultures.FirstOrDefault(c => content.IsPublished(c.Key)).Key;
        return string.IsNullOrWhiteSpace(published) ? null : published;
    }

    private static bool IsMainPage(IPublishedContent content)
    {
        if (content.ContentType.Alias.Equals("xmlSiteMap", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!content.IsVisible())
        {
            return false;
        }

        if (content.HasProperty("hideFromXmlSitemap") && content.Value<bool>("hideFromXmlSitemap"))
        {
            return false;
        }

        if (content.HasProperty("hideFromXmlSiteMap") && content.Value<bool>("hideFromXmlSiteMap"))
        {
            return false;
        }

        return true;
    }

    private static bool IsValidUrl(string? url)
        => !string.IsNullOrWhiteSpace(url) && url != "#";

    private static string FormatLink(LlmLink link)
        => string.IsNullOrWhiteSpace(link.Description)
            ? $"- [{link.Title}]({link.Url})"
            : $"- [{link.Title}]({link.Url}): {link.Description}";

    private sealed record LlmLink(string Title, string Url, string? Description);

    private sealed record LlmsContent(List<LlmLink> KeyPages, List<LlmLink> Brands)
    {
        public static LlmsContent Empty { get; } = new(new List<LlmLink>(), new List<LlmLink>());
    }

    private sealed record LlmsCultureContent(string? Culture, LlmsContent Content);
}
