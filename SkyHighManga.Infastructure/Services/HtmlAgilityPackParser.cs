using HtmlAgilityPack;
using SkyHighManga.Application.Interfaces.Services;

namespace SkyHighManga.Infrastructure.Services;

/// <summary>
/// Implementation của IHtmlParser sử dụng HtmlAgilityPack
/// </summary>
public class HtmlAgilityPackParser : IHtmlParser
{
    public IHtmlDocument Parse(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return new HtmlDocumentWrapper(doc);
    }

    public string? GetText(string html, string selector)
    {
        var doc = Parse(html);
        var element = doc.QuerySelector(selector);
        return element?.TextContent?.Trim();
    }

    public string? GetAttribute(string html, string selector, string attributeName)
    {
        var doc = Parse(html);
        var element = doc.QuerySelector(selector);
        return element?.GetAttribute(attributeName);
    }

    public IEnumerable<string> GetTexts(string html, string selector)
    {
        var doc = Parse(html);
        return doc.QuerySelectorAll(selector)
            .Select(e => e.TextContent?.Trim())
            .Where(text => !string.IsNullOrEmpty(text))
            .Cast<string>();
    }

    public IEnumerable<string> GetAttributes(string html, string selector, string attributeName)
    {
        var doc = Parse(html);
        return doc.QuerySelectorAll(selector)
            .Select(e => e.GetAttribute(attributeName))
            .Where(attr => !string.IsNullOrEmpty(attr))
            .Cast<string>();
    }
}

/// <summary>
/// Wrapper cho HtmlDocument của HtmlAgilityPack
/// </summary>
internal class HtmlDocumentWrapper : IHtmlDocument
{
    private readonly HtmlDocument _document;

    public HtmlDocumentWrapper(HtmlDocument document)
    {
        _document = document;
    }

    public string? TextContent => _document.DocumentNode?.InnerText?.Trim();

    public IHtmlElement? QuerySelector(string selector)
    {
        var node = HtmlSelectorHelper.SelectNode(_document.DocumentNode, selector);
        return node != null ? new HtmlElementWrapper(node) : null;
    }

    public IEnumerable<IHtmlElement> QuerySelectorAll(string selector)
    {
        var nodes = HtmlSelectorHelper.SelectNodes(_document.DocumentNode, selector);
        if (nodes == null)
            return Enumerable.Empty<IHtmlElement>();

        return nodes.Select(node => new HtmlElementWrapper(node));
    }
}

/// <summary>
/// Wrapper cho HtmlNode của HtmlAgilityPack
/// </summary>
internal class HtmlElementWrapper : IHtmlElement
{
    private readonly HtmlNode _node;

    public HtmlElementWrapper(HtmlNode node)
    {
        _node = node;
    }

    public string? TextContent => _node.InnerText?.Trim();

    public string? InnerHtml => _node.InnerHtml;

    public string? OuterHtml => _node.OuterHtml;

    public string? GetAttribute(string name)
    {
        return _node.GetAttributeValue(name, null);
    }

    public IHtmlElement? QuerySelector(string selector)
    {
        var node = HtmlSelectorHelper.SelectNode(_node, selector);
        return node != null ? new HtmlElementWrapper(node) : null;
    }

    public IEnumerable<IHtmlElement> QuerySelectorAll(string selector)
    {
        var nodes = HtmlSelectorHelper.SelectNodes(_node, selector);
        if (nodes == null)
            return Enumerable.Empty<IHtmlElement>();

        return nodes.Select(node => new HtmlElementWrapper(node));
    }
}

