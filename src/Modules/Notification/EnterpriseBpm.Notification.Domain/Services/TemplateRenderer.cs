using System.Net;
using System.Text.RegularExpressions;

namespace EnterpriseBpm.Notification.Domain.Services;

public static class TemplateRenderer
{
    private static readonly Regex TokenRegex = new(@"\{\{\s*([\w\.\-]+)\s*\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Biên dịch template chuỗi thay thế các token {{ key }} bằng dữ liệu tương ứng.
    /// Khử nhiễm mã độc HTML (HTML Sanitization) bằng cách encode các giá trị đầu vào.
    /// </summary>
    public static string Render(string template, IDictionary<string, object?> parameters, bool sanitizeHtml = true)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;
        if (parameters == null || parameters.Count == 0) return template;

        return TokenRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value.Trim();
            if (parameters.TryGetValue(key, out var val) && val != null)
            {
                var strVal = val.ToString() ?? string.Empty;
                return sanitizeHtml ? WebUtility.HtmlEncode(strVal) : strVal;
            }
            return string.Empty;
        });
    }
}

