using System.Net;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Strings;

namespace PromomarkWeb.Helpers
{
    public static class HtmlHelpers
    {
        public static bool IsNullOrDefault(this object? obj, bool toStringable = false)
        {
                var res = obj == null || obj.Equals(obj.GetType().GetDefaultValue());

                if (!res && toStringable)
                {
                    res = obj!.ToString().IsNullOrWhiteSpace();
                }

                return res;
            }

        // Removes a single outer <p>...</p> wrapper if the entire HTML is wrapped in it (attributes allowed)
        public static string RemoveOuterPTags(this string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var input = html.Trim();
            // Match a single outer <p ...>...</p> with any attributes, across lines
            var pattern = @"^\s*<p\b[^>]*>([\s\S]*?)<\/p>\s*$";
            var result = Regex.Replace(input, pattern, "$1", RegexOptions.IgnoreCase);
            return result;
        }

        // Overload for Umbraco's IHtmlEncodedString so views can call directly without Html.Raw
        public static IHtmlEncodedString RemoveOuterPTags(this IHtmlEncodedString? html)
        {
            var s = html?.ToHtmlString() ?? string.Empty;
            var cleaned = RemoveOuterPTags(s);
            return new HtmlEncodedString(cleaned);
        }

        public static string StripHtml(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var text = Regex.Replace(input, "<.*?>", string.Empty);
            return WebUtility.HtmlDecode(text).Trim();
        }
    }
}