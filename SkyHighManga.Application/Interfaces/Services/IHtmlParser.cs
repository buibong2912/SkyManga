namespace SkyHighManga.Application.Interfaces.Services;

/// <summary>
/// Service để parse HTML
/// </summary>
public interface IHtmlParser
{
    /// <summary>
    /// Parse HTML string thành document
    /// </summary>
    IHtmlDocument Parse(string html);

    /// <summary>
    /// Lấy text từ selector
    /// </summary>
    string? GetText(string html, string selector);

    /// <summary>
    /// Lấy attribute từ selector
    /// </summary>
    string? GetAttribute(string html, string selector, string attributeName);

    /// <summary>
    /// Lấy danh sách text từ selector
    /// </summary>
    IEnumerable<string> GetTexts(string html, string selector);

    /// <summary>
    /// Lấy danh sách attributes từ selector
    /// </summary>
    IEnumerable<string> GetAttributes(string html, string selector, string attributeName);
}

/// <summary>
/// HTML Document interface
/// </summary>
public interface IHtmlDocument
{
    /// <summary>
    /// Query selector
    /// </summary>
    IHtmlElement? QuerySelector(string selector);

    /// <summary>
    /// Query selector all
    /// </summary>
    IEnumerable<IHtmlElement> QuerySelectorAll(string selector);

    /// <summary>
    /// Lấy text content
    /// </summary>
    string? TextContent { get; }
}

/// <summary>
/// HTML Element interface
/// </summary>
public interface IHtmlElement
{
    /// <summary>
    /// Text content
    /// </summary>
    string? TextContent { get; }

    /// <summary>
    /// Inner HTML
    /// </summary>
    string? InnerHtml { get; }

    /// <summary>
    /// Outer HTML
    /// </summary>
    string? OuterHtml { get; }

    /// <summary>
    /// Lấy attribute
    /// </summary>
    string? GetAttribute(string name);

    /// <summary>
    /// Query selector
    /// </summary>
    IHtmlElement? QuerySelector(string selector);

    /// <summary>
    /// Query selector all
    /// </summary>
    IEnumerable<IHtmlElement> QuerySelectorAll(string selector);
}

