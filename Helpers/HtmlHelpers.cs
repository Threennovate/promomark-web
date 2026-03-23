using System.Net;
using System.Security;
using System.Text;
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

        public static string EmailToImageTag(this string? email, string cssClass = "", string textColor = "#111827", int fontSize = 20)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;

            var value = email.Trim();
            var escapedText = SecurityElement.Escape(value) ?? string.Empty;
            var escapedColor = SecurityElement.Escape(textColor) ?? "#111827";

            var width = Math.Max(120, (int)Math.Ceiling(value.Length * (fontSize * 0.66)) + 8);
            var height = Math.Max(22, (int)Math.Ceiling(fontSize * 1.45));
            var baseline = (int)Math.Ceiling(height * 0.78);

            var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'><text x='0' y='{baseline}' font-family='Arial, Helvetica, sans-serif' font-size='{fontSize}' fill='{escapedColor}'>{escapedText}</text></svg>";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
            var classAttribute = string.IsNullOrWhiteSpace(cssClass) ? string.Empty : $" class=\"{WebUtility.HtmlEncode(cssClass)}\"";

            return $"<img src=\"data:image/svg+xml;base64,{base64}\"{classAttribute} alt=\"\" aria-hidden=\"true\" decoding=\"async\" />";
        }
    }
}