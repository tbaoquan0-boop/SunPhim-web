using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SunPhim.Helpers;

public static partial class SlugHelper
{
    private static readonly Regex NonAlphanumeric = NonAlphanumericRegex();
    private static readonly Regex MultipleHyphens = MultipleHyphensRegex();

    public static string ToSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string slug = text.ToLowerInvariant().Trim();

        slug = RemoveDiacritics(slug);

        slug = NonAlphanumeric.Replace(slug, "-");

        slug = MultipleHyphens.Replace(slug, "-");

        slug = slug.Trim('-');

        if (slug.Length > 100) slug = slug[..100].TrimEnd('-');

        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }

    public static string ToUrl(string baseUrl, string slug, string? suffix = null)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{slug}";
        if (!string.IsNullOrWhiteSpace(suffix)) url += $"-{suffix}";
        return url;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString()
            .Replace("đ", "d")
            .Replace("Đ", "d")
            .Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^a-z0-9\-]", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"\-+", RegexOptions.Compiled)]
    private static partial Regex MultipleHyphensRegex();
}
