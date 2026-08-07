using System;
using Ganss.Xss;
using Kilavuz.Web.Application.Interfaces;

namespace Kilavuz.Web.Infrastructure.Security;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();

        // 1. Clear default tags/attributes to be explicit
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedCssProperties.Clear(); // Can be added later if needed

        // 2. White-listed Tags for Summernote / Rich Text
        var allowedTags = new[]
        {
            "p", "b", "i", "u", "strong", "em", "a", "ul", "ol", "li",
            "h1", "h2", "h3", "h4", "h5", "h6", "br", "hr", 
            "table", "thead", "tbody", "tr", "th", "td", 
            "img", "span", "div", "blockquote", "pre", "code"
        };
        foreach (var tag in allowedTags)
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        // 3. White-listed Attributes
        var allowedAttributes = new[]
        {
            "href", "src", "alt", "title", "class", "style", "target"
        };
        foreach (var attr in allowedAttributes)
        {
            _sanitizer.AllowedAttributes.Add(attr);
        }

        // 4. Force img src to be relative and start with /uploads/images/
        _sanitizer.FilterUrl += (sender, args) =>
        {
            if (string.Equals(args.OriginalUrl, args.SanitizedUrl, StringComparison.OrdinalIgnoreCase))
            {
                // Only intercept img src
                if (args.Tag.TagName.Equals("img", StringComparison.OrdinalIgnoreCase))
                {
                    if (!args.SanitizedUrl.StartsWith("/uploads/images/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Reject external or unauthorized internal images
                        args.SanitizedUrl = null; 
                    }
                }
            }
        };
    }

    public string Sanitize(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return htmlContent;

        return _sanitizer.Sanitize(htmlContent);
    }
}
