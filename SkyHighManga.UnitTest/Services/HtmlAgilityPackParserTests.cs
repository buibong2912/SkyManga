using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Infastructure.Services;

namespace SkyHighManga.UnitTest.Services;

/// <summary>
/// Unit tests cho HtmlAgilityPackParser
/// </summary>
[TestFixture]
public class HtmlAgilityPackParserTests
{
    private IHtmlParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        _parser = new HtmlAgilityPackParser();
        Console.WriteLine("=".PadRight(80, '='));
    }

    [TearDown]
    public void TearDown()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();
    }

    #region Parse Tests

    [Test]
    public void Parse_ValidHtml_ReturnsDocument()
    {
        // Arrange
        var html = "<html><body><h1>Test</h1></body></html>";

        // Act
        var document = _parser.Parse(html);

        // Assert
        Console.WriteLine($"Test: Parse_ValidHtml_ReturnsDocument");
        Console.WriteLine($"Document is null: {document == null}");
        Console.WriteLine($"TextContent: {document?.TextContent}");
        Assert.That(document, Is.Not.Null);
        Assert.That(document.TextContent, Is.Not.Null);
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void Parse_EmptyHtml_ReturnsDocument()
    {
        // Arrange
        var html = "";

        // Act
        var document = _parser.Parse(html);

        // Assert
        Console.WriteLine($"Test: Parse_EmptyHtml_ReturnsDocument");
        Console.WriteLine($"Document is null: {document == null}");
        Assert.That(document, Is.Not.Null);
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void Parse_InvalidHtml_ReturnsDocument()
    {
        // Arrange
        var html = "<div><p>Unclosed tag";

        // Act
        var document = _parser.Parse(html);

        // Assert
        Console.WriteLine($"Test: Parse_InvalidHtml_ReturnsDocument");
        Console.WriteLine($"Document is null: {document == null}");
        Assert.That(document, Is.Not.Null);
        Console.WriteLine("✓ Test passed\n");
    }

    #endregion

    #region GetText Tests

    [Test]
    public void GetText_ValidSelector_ReturnsText()
    {
        // Arrange
        var html = "<html><body><h1 class='title'>Hello World</h1></body></html>";

        // Act
        var text = _parser.GetText(html, "//h1");

        // Assert
        Console.WriteLine($"Test: GetText_ValidSelector_ReturnsText");
        Console.WriteLine($"Result: {text}");
        Assert.That(text, Is.EqualTo("Hello World"));
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void GetText_WithWhitespace_ReturnsTrimmedText()
    {
        // Arrange
        var html = "<html><body><h1>   Hello World   </h1></body></html>";

        // Act
        var text = _parser.GetText(html, "//h1");

        // Assert
        Console.WriteLine($"Test: GetText_WithWhitespace_ReturnsTrimmedText");
        Console.WriteLine($"Original: '   Hello World   '");
        Console.WriteLine($"Result: '{text}'");
        Assert.That(text, Is.EqualTo("Hello World"));
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void GetText_SelectorNotFound_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><h1>Hello</h1></body></html>";

        // Act
        var text = _parser.GetText(html, "//div[@class='notfound']");

        // Assert
        Console.WriteLine($"Test: GetText_SelectorNotFound_ReturnsNull");
        Console.WriteLine($"Result: {text ?? "null"}");
        Assert.That(text, Is.Null);
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void GetText_EmptyElement_ReturnsEmptyString()
    {
        // Arrange
        var html = "<html><body><div></div></body></html>";

        // Act
        var text = _parser.GetText(html, "//div");

        // Assert
        Console.WriteLine($"Test: GetText_EmptyElement_ReturnsEmptyString");
        Console.WriteLine($"Result: '{text ?? "null"}'");
        Assert.That(text, Is.EqualTo(""));
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void GetText_ComplexHtml_ReturnsCorrectText()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <div class='manga-title'>
                        <h1>One Piece</h1>
                        <span class='author'>Eiichiro Oda</span>
                    </div>
                </body>
            </html>";

        // Act
        var title = _parser.GetText(html, "//h1");
        var author = _parser.GetText(html, "//span[@class='author']");

        // Assert
        Console.WriteLine($"Test: GetText_ComplexHtml_ReturnsCorrectText");
        Console.WriteLine($"Title: {title}");
        Console.WriteLine($"Author: {author}");
        Assert.That(title, Is.EqualTo("One Piece"));
        Assert.That(author, Is.EqualTo("Eiichiro Oda"));
        Console.WriteLine("✓ Test passed\n");
    }

    #endregion

    #region GetAttribute Tests

    [Test]
    public void GetAttribute_ValidAttribute_ReturnsValue()
    {
        // Arrange
        var html = "<html><body><a href='https://example.com'>Link</a></body></html>";

        // Act
        var href = _parser.GetAttribute(html, "//a", "href");

        // Assert
        Console.WriteLine($"Test: GetAttribute_ValidAttribute_ReturnsValue");
        Console.WriteLine($"Attribute 'href': {href}");
        Assert.That(href, Is.EqualTo("https://example.com"));
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void GetAttribute_ImageSrc_ReturnsSrc()
    {
        // Arrange
        var html = "<html><body><img src='/images/cover.jpg' alt='Cover' /></body></html>";

        // Act
        var src = _parser.GetAttribute(html, "//img", "src");
        var alt = _parser.GetAttribute(html, "//img", "alt");

        // Assert
        Console.WriteLine($"Test: GetAttribute_ImageSrc_ReturnsSrc");
        Console.WriteLine($"src: {src}");
        Console.WriteLine($"alt: {alt}");
        Assert.That(src, Is.EqualTo("/images/cover.jpg"));
        Assert.That(alt, Is.EqualTo("Cover"));
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void GetAttribute_AttributeNotFound_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><div>Test</div></body></html>";

        // Act
        var href = _parser.GetAttribute(html, "//div", "href");

        // Assert
        Assert.That(href, Is.Null);
    }

    [Test]
    public void GetAttribute_ElementNotFound_ReturnsNull()
    {
        // Arrange
        var html = "<html><body><div>Test</div></body></html>";

        // Act
        var href = _parser.GetAttribute(html, "//a", "href");

        // Assert
        Assert.That(href, Is.Null);
    }

    [Test]
    public void GetAttribute_MultipleAttributes_ReturnsCorrectOne()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <img src='/cover.jpg' alt='Cover' class='manga-cover' data-id='123' />
                </body>
            </html>";

        // Act
        var src = _parser.GetAttribute(html, "//img", "src");
        var alt = _parser.GetAttribute(html, "//img", "alt");
        var className = _parser.GetAttribute(html, "//img", "class");
        var dataId = _parser.GetAttribute(html, "//img", "data-id");

        // Assert
        Console.WriteLine($"Test: GetAttribute_MultipleAttributes_ReturnsCorrectOne");
        Console.WriteLine($"src: {src}");
        Console.WriteLine($"alt: {alt}");
        Console.WriteLine($"class: {className}");
        Console.WriteLine($"data-id: {dataId}");
        Assert.That(src, Is.EqualTo("/cover.jpg"));
        Assert.That(alt, Is.EqualTo("Cover"));
        Assert.That(className, Is.EqualTo("manga-cover"));
        Assert.That(dataId, Is.EqualTo("123"));
        Console.WriteLine("✓ Test passed\n");
    }

    #endregion

    #region GetTexts Tests

    [Test]
    public void GetTexts_MultipleElements_ReturnsAllTexts()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <ul>
                        <li>Item 1</li>
                        <li>Item 2</li>
                        <li>Item 3</li>
                    </ul>
                </body>
            </html>";

        // Act
        var texts = _parser.GetTexts(html, "//li").ToList();

        // Assert
        Console.WriteLine($"Test: GetTexts_MultipleElements_ReturnsAllTexts");
        Console.WriteLine($"Found {texts.Count} items:");
        foreach (var text in texts)
        {
            Console.WriteLine($"  - {text}");
        }
        Assert.That(texts, Has.Count.EqualTo(3));
        Assert.That(texts, Contains.Item("Item 1"));
        Assert.That(texts, Contains.Item("Item 2"));
        Assert.That(texts, Contains.Item("Item 3"));
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void GetTexts_EmptyElements_ReturnsEmptyList()
    {
        // Arrange
        var html = "<html><body><ul><li></li><li></li></ul></body></html>";

        // Act
        var texts = _parser.GetTexts(html, "//li").ToList();

        // Assert
        Assert.That(texts, Is.Empty);
    }

    [Test]
    public void GetTexts_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var html = "<html><body><div>Test</div></body></html>";

        // Act
        var texts = _parser.GetTexts(html, "//li").ToList();

        // Assert
        Assert.That(texts, Is.Empty);
    }

    [Test]
    public void GetTexts_ChapterList_ReturnsAllChapters()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <div class='chapter-list'>
                        <a href='/chapter/1'>Chapter 1</a>
                        <a href='/chapter/2'>Chapter 2</a>
                        <a href='/chapter/3'>Chapter 3</a>
                    </div>
                </body>
            </html>";

        // Act
        var chapters = _parser.GetTexts(html, "//a[@href]").ToList();

        // Assert
        Assert.That(chapters, Has.Count.EqualTo(3));
        Assert.That(chapters, Contains.Item("Chapter 1"));
        Assert.That(chapters, Contains.Item("Chapter 2"));
        Assert.That(chapters, Contains.Item("Chapter 3"));
    }

    [Test]
    public void GetTexts_WithWhitespace_ReturnsTrimmedTexts()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <div>   Text 1   </div>
                    <div>   Text 2   </div>
                </body>
            </html>";

        // Act
        var texts = _parser.GetTexts(html, "//div").ToList();

        // Assert
        Assert.That(texts, Has.Count.EqualTo(2));
        Assert.That(texts, Contains.Item("Text 1"));
        Assert.That(texts, Contains.Item("Text 2"));
    }

    #endregion

    #region GetAttributes Tests

    [Test]
    public void GetAttributes_MultipleElements_ReturnsAllAttributes()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <a href='/chapter/1'>Chapter 1</a>
                    <a href='/chapter/2'>Chapter 2</a>
                    <a href='/chapter/3'>Chapter 3</a>
                </body>
            </html>";

        // Act
        var hrefs = _parser.GetAttributes(html, "//a", "href").ToList();

        // Assert
        Console.WriteLine($"Test: GetAttributes_MultipleElements_ReturnsAllAttributes");
        Console.WriteLine($"Found {hrefs.Count} hrefs:");
        foreach (var href in hrefs)
        {
            Console.WriteLine($"  - {href}");
        }
        Assert.That(hrefs, Has.Count.EqualTo(3));
        Assert.That(hrefs, Contains.Item("/chapter/1"));
        Assert.That(hrefs, Contains.Item("/chapter/2"));
        Assert.That(hrefs, Contains.Item("/chapter/3"));
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void GetAttributes_SomeMissingAttributes_ReturnsOnlyExisting()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <a href='/chapter/1'>Chapter 1</a>
                    <a>Chapter 2</a>
                    <a href='/chapter/3'>Chapter 3</a>
                </body>
            </html>";

        // Act
        var hrefs = _parser.GetAttributes(html, "//a", "href").ToList();

        // Assert
        Assert.That(hrefs, Has.Count.EqualTo(2));
        Assert.That(hrefs, Contains.Item("/chapter/1"));
        Assert.That(hrefs, Contains.Item("/chapter/3"));
    }

    [Test]
    public void GetAttributes_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var html = "<html><body><div>Test</div></body></html>";

        // Act
        var hrefs = _parser.GetAttributes(html, "//a", "href").ToList();

        // Assert
        Assert.That(hrefs, Is.Empty);
    }

    [Test]
    public void GetAttributes_ImageSources_ReturnsAllSrcs()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <div class='pages'>
                        <img src='/page1.jpg' />
                        <img src='/page2.jpg' />
                        <img src='/page3.jpg' />
                    </div>
                </body>
            </html>";

        // Act
        var srcs = _parser.GetAttributes(html, "//img", "src").ToList();

        // Assert
        Assert.That(srcs, Has.Count.EqualTo(3));
        Assert.That(srcs, Contains.Item("/page1.jpg"));
        Assert.That(srcs, Contains.Item("/page2.jpg"));
        Assert.That(srcs, Contains.Item("/page3.jpg"));
    }

    #endregion

    #region Document QuerySelector Tests

    [Test]
    public void Document_QuerySelector_ReturnsElement()
    {
        // Arrange
        var html = "<html><body><div class='test'>Content</div></body></html>";
        var document = _parser.Parse(html);

        // Act
        var element = document.QuerySelector("//div[@class='test']");

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element!.TextContent, Is.EqualTo("Content"));
    }

    [Test]
    public void Document_QuerySelectorAll_ReturnsAllElements()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <div class='item'>Item 1</div>
                    <div class='item'>Item 2</div>
                    <div class='item'>Item 3</div>
                </body>
            </html>";
        var document = _parser.Parse(html);

        // Act
        var elements = document.QuerySelectorAll("//div[@class='item']").ToList();

        // Assert
        Assert.That(elements, Has.Count.EqualTo(3));
        Assert.That(elements[0].TextContent, Is.EqualTo("Item 1"));
        Assert.That(elements[1].TextContent, Is.EqualTo("Item 2"));
        Assert.That(elements[2].TextContent, Is.EqualTo("Item 3"));
    }

    [Test]
    public void Document_TextContent_ReturnsAllText()
    {
        // Arrange
        var html = "<html><body><h1>Title</h1><p>Paragraph</p></body></html>";
        var document = _parser.Parse(html);

        // Act
        var textContent = document.TextContent;

        // Assert
        Assert.That(textContent, Is.Not.Null);
        Assert.That(textContent, Contains.Substring("Title"));
        Assert.That(textContent, Contains.Substring("Paragraph"));
    }

    #endregion

    #region Element QuerySelector Tests

    [Test]
    public void Element_QuerySelector_ReturnsNestedElement()
    {
        // Arrange
        var html = @"
            <html>
                <body>
                    <div class='manga'>
                        <h1 class='title'>One Piece</h1>
                        <span class='author'>Oda</span>
                    </div>
                </body>
            </html>";
        var document = _parser.Parse(html);
        var mangaDiv = document.QuerySelector("//div[@class='manga']");

        // Act
        var title = mangaDiv!.QuerySelector("//h1[@class='title']");
        var author = mangaDiv.QuerySelector("//span[@class='author']");

        // Assert
        Assert.That(title, Is.Not.Null);
        Assert.That(title!.TextContent, Is.EqualTo("One Piece"));
        Assert.That(author, Is.Not.Null);
        Assert.That(author!.TextContent, Is.EqualTo("Oda"));
    }

    [Test]
    public void Element_GetAttribute_ReturnsAttribute()
    {
        // Arrange
        var html = "<html><body><a href='/test' class='link'>Link</a></body></html>";
        var document = _parser.Parse(html);
        var link = document.QuerySelector("//a");

        // Act
        var href = link!.GetAttribute("href");
        var className = link.GetAttribute("class");

        // Assert
        Assert.That(href, Is.EqualTo("/test"));
        Assert.That(className, Is.EqualTo("link"));
    }

    [Test]
    public void Element_InnerHtml_ReturnsInnerHtml()
    {
        // Arrange
        var html = "<html><body><div><p>Test</p></div></body></html>";
        var document = _parser.Parse(html);
        var div = document.QuerySelector("//div");

        // Act
        var innerHtml = div!.InnerHtml;

        // Assert
        Assert.That(innerHtml, Is.Not.Null);
        Assert.That(innerHtml, Contains.Substring("<p>Test</p>"));
    }

    [Test]
    public void Element_OuterHtml_ReturnsOuterHtml()
    {
        // Arrange
        var html = "<html><body><div class='test'>Content</div></body></html>";
        var document = _parser.Parse(html);
        var div = document.QuerySelector("//div");

        // Act
        var outerHtml = div!.OuterHtml;

        // Assert
        Assert.That(outerHtml, Is.Not.Null);
        Assert.That(outerHtml, Contains.Substring("class='test'"));
        Assert.That(outerHtml, Contains.Substring("Content"));
    }

    #endregion

    #region Real World Scenarios

    [Test]
    public void ParseMangaPage_ExtractsMangaInfo()
    {
        // Arrange - Simulate a real manga page HTML
        var html = @"
            <html>
                <body>
                    <div class='manga-detail'>
                        <h1 class='manga-title'>One Piece</h1>
                        <div class='manga-info'>
                            <span class='author'>Author: Eiichiro Oda</span>
                            <span class='status'>Status: Ongoing</span>
                            <div class='genres'>
                                <a href='/genre/action'>Action</a>
                                <a href='/genre/adventure'>Adventure</a>
                            </div>
                        </div>
                        <img class='cover-image' src='/covers/onepiece.jpg' alt='One Piece Cover' />
                    </div>
                </body>
            </html>";

        // Act
        var title = _parser.GetText(html, "//h1[@class='manga-title']");
        var author = _parser.GetText(html, "//span[@class='author']");
        var status = _parser.GetText(html, "//span[@class='status']");
        var coverUrl = _parser.GetAttribute(html, "//img[@class='cover-image']", "src");
        var genres = _parser.GetTexts(html, "//div[@class='genres']//a").ToList();

        // Assert
        Console.WriteLine($"Test: ParseMangaPage_ExtractsMangaInfo");
        Console.WriteLine($"Title: {title}");
        Console.WriteLine($"Author: {author}");
        Console.WriteLine($"Status: {status}");
        Console.WriteLine($"Cover URL: {coverUrl}");
        Console.WriteLine($"Genres ({genres.Count}): {string.Join(", ", genres)}");
        
        Assert.That(title, Is.EqualTo("One Piece"));
        Assert.That(author, Is.EqualTo("Author: Eiichiro Oda"));
        Assert.That(status, Is.EqualTo("Status: Ongoing"));
        Assert.That(coverUrl, Is.EqualTo("/covers/onepiece.jpg"));
        Assert.That(genres, Has.Count.EqualTo(2));
        Assert.That(genres, Contains.Item("Action"));
        Assert.That(genres, Contains.Item("Adventure"));
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void ParseChapterList_ExtractsAllChapters()
    {
        // Arrange - Simulate a chapter list page
        var html = @"
            <html>
                <body>
                    <ul class='chapter-list'>
                        <li>
                            <a href='/manga/123/chapter/1' class='chapter-link'>
                                Chapter 1: Romance Dawn
                            </a>
                            <span class='chapter-date'>2024-01-01</span>
                        </li>
                        <li>
                            <a href='/manga/123/chapter/2' class='chapter-link'>
                                Chapter 2: Against Alvida
                            </a>
                            <span class='chapter-date'>2024-01-08</span>
                        </li>
                    </ul>
                </body>
            </html>";

        // Act
        var chapterLinks = _parser.GetAttributes(html, "//a[@class='chapter-link']", "href").ToList();
        var chapterTitles = _parser.GetTexts(html, "//a[@class='chapter-link']").ToList();
        var chapterDates = _parser.GetTexts(html, "//span[@class='chapter-date']").ToList();

        // Assert
        Console.WriteLine($"Test: ParseChapterList_ExtractsAllChapters");
        Console.WriteLine($"Found {chapterLinks.Count} chapters:");
        for (int i = 0; i < chapterLinks.Count; i++)
        {
            Console.WriteLine($"  Chapter {i + 1}:");
            Console.WriteLine($"    Link: {chapterLinks[i]}");
            Console.WriteLine($"    Title: {chapterTitles[i]?.Trim()}");
            Console.WriteLine($"    Date: {chapterDates[i]}");
        }
        
        Assert.That(chapterLinks, Has.Count.EqualTo(2));
        Assert.That(chapterLinks[0], Is.EqualTo("/manga/123/chapter/1"));
        Assert.That(chapterLinks[1], Is.EqualTo("/manga/123/chapter/2"));
        
        Assert.That(chapterTitles, Has.Count.EqualTo(2));
        Assert.That(chapterTitles[0], Contains.Substring("Chapter 1"));
        Assert.That(chapterTitles[1], Contains.Substring("Chapter 2"));
        
        Assert.That(chapterDates, Has.Count.EqualTo(2));
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void ParsePageList_ExtractsAllPageUrls()
    {
        // Arrange - Simulate a chapter page with images
        var html = @"
            <html>
                <body>
                    <div class='reader-content'>
                        <img src='/images/chapter1/page1.jpg' class='page-image' />
                        <img src='/images/chapter1/page2.jpg' class='page-image' />
                        <img src='/images/chapter1/page3.jpg' class='page-image' />
                    </div>
                </body>
            </html>";

        // Act
        var pageUrls = _parser.GetAttributes(html, "//img[@class='page-image']", "src").ToList();

        // Assert
        Console.WriteLine($"Test: ParsePageList_ExtractsAllPageUrls");
        Console.WriteLine($"Found {pageUrls.Count} page URLs:");
        for (int i = 0; i < pageUrls.Count; i++)
        {
            Console.WriteLine($"  Page {i + 1}: {pageUrls[i]}");
        }
        
        Assert.That(pageUrls, Has.Count.EqualTo(3));
        Assert.That(pageUrls[0], Is.EqualTo("/images/chapter1/page1.jpg"));
        Assert.That(pageUrls[1], Is.EqualTo("/images/chapter1/page2.jpg"));
        Assert.That(pageUrls[2], Is.EqualTo("/images/chapter1/page3.jpg"));
        Console.WriteLine("✓ Test passed\n");
    }

    #endregion
}

