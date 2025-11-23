using HtmlAgilityPack;

namespace SkyHighManga.Infrastructure.Services;

/// <summary>
/// Helper để convert CSS selector sang XPath cho HtmlAgilityPack
/// </summary>
internal static class HtmlSelectorHelper
{
    /// <summary>
    /// Convert CSS selector sang XPath (basic support)
    /// HtmlAgilityPack hỗ trợ XPath, nhưng có thể convert một số CSS selector đơn giản
    /// </summary>
    public static string CssToXPath(string cssSelector)
    {
        if (string.IsNullOrWhiteSpace(cssSelector))
            return "//*";

        // Nếu đã là XPath (bắt đầu với // hoặc /), trả về nguyên
        if (cssSelector.StartsWith("//") || cssSelector.StartsWith("/"))
            return cssSelector;

        // Convert một số CSS selector phổ biến sang XPath
        var xpath = cssSelector
            // Class selector: .class -> [@class='class']
            .Replace(".", "[@class='")
            // ID selector: #id -> [@id='id']
            .Replace("#", "[@id='")
            // Tag selector: tag -> tag
            ;

        // Xử lý attribute selector: [attr='value'] -> [@attr='value']
        xpath = System.Text.RegularExpressions.Regex.Replace(
            xpath,
            @"\[([^\]]+)\]",
            match => "[@" + match.Groups[1].Value + "]"
        );

        // Nếu không có // ở đầu, thêm vào
        if (!xpath.StartsWith("//"))
        {
            // Nếu bắt đầu với tag name, thêm //
            if (char.IsLetter(xpath[0]) || xpath[0] == '*')
            {
                xpath = "//" + xpath;
            }
            else
            {
                xpath = "//*" + xpath;
            }
        }

        return xpath;
    }

    /// <summary>
    /// Select node sử dụng CSS selector hoặc XPath
    /// </summary>
    public static HtmlNode? SelectNode(HtmlNode node, string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return null;

        // Nếu là XPath relative (bắt đầu với .// hoặc ./)
        if (selector.StartsWith(".//") || selector.StartsWith("./"))
        {
            return node.SelectSingleNode(selector);
        }

        // Nếu là XPath absolute (bắt đầu với // hoặc /)
        if (selector.StartsWith("//") || selector.StartsWith("/"))
        {
            return node.SelectSingleNode(selector);
        }

        // Thử convert CSS selector sang XPath
        try
        {
            var xpath = CssToXPath(selector);
            return node.SelectSingleNode(xpath);
        }
        catch
        {
            // Nếu không convert được, thử dùng trực tiếp như XPath
            try
            {
                return node.SelectSingleNode(selector);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Select nodes sử dụng CSS selector hoặc XPath
    /// </summary>
    public static HtmlNodeCollection? SelectNodes(HtmlNode node, string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return null;

        // Nếu là XPath relative (bắt đầu với .// hoặc ./)
        if (selector.StartsWith(".//") || selector.StartsWith("./"))
        {
            return node.SelectNodes(selector);
        }

        // Nếu là XPath absolute (bắt đầu với // hoặc /)
        if (selector.StartsWith("//") || selector.StartsWith("/"))
        {
            return node.SelectNodes(selector);
        }

        // Thử convert CSS selector sang XPath
        try
        {
            var xpath = CssToXPath(selector);
            return node.SelectNodes(xpath);
        }
        catch
        {
            // Nếu không convert được, thử dùng trực tiếp như XPath
            try
            {
                return node.SelectNodes(selector);
            }
            catch
            {
                return null;
            }
        }
    }
}

