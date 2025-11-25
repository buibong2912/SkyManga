using System.Collections.Concurrent;
using SkyHighManga.Application.Interfaces.Crawlers;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Crawlers;

/// <summary>
/// Factory để tạo crawler instances
/// </summary>
public class CrawlerFactory : ICrawlerFactory
{
    private readonly IHtmlParser _htmlParser;
    private readonly ConcurrentDictionary<string, Type> _registeredCrawlers = new();

    public CrawlerFactory(IHtmlParser htmlParser)
    {
        _htmlParser = htmlParser;
    }

    public ICrawler? CreateCrawler(Source source)
    {
        if (string.IsNullOrEmpty(source.CrawlerClassName))
        {
            // Nếu không có crawler class name, thử detect từ base URL
            return DetectCrawlerFromSource(source);
        }

        return CreateCrawler(source.CrawlerClassName);
    }

    public ICrawler? CreateCrawler(string crawlerClassName)
    {
        if (_registeredCrawlers.TryGetValue(crawlerClassName, out var crawlerType))
        {
            return CreateCrawlerInstance(crawlerType);
        }

        // Fallback: thử tìm trong assembly
        var assembly = typeof(CrawlerFactory).Assembly;
        var type = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == crawlerClassName && typeof(ICrawler).IsAssignableFrom(t));
        if (type != null)
        {
            return CreateCrawlerInstance(type);
        }

        return null;
    }

    public IMangaCrawler? CreateMangaCrawler(Source source)
    {
        var crawler = CreateCrawler(source);
        return crawler as IMangaCrawler;
    }

    public IChapterCrawler? CreateChapterCrawler(Source source)
    {
        var crawler = CreateCrawler(source);
        return crawler as IChapterCrawler;
    }

    public IPageCrawler? CreatePageCrawler(Source source)
    {
        var crawler = CreateCrawler(source);
        return crawler as IPageCrawler;
    }

    public void RegisterCrawler<T>(string name) where T : class, ICrawler
    {
        _registeredCrawlers[name] = typeof(T);
    }

    public IEnumerable<string> GetRegisteredCrawlers()
    {
        return _registeredCrawlers.Keys;
    }

    private ICrawler? DetectCrawlerFromSource(Source source)
    {
        // Detect crawler từ base URL
        if (source.BaseUrl.Contains("nettruyen") || source.BaseUrl.Contains("aquastarsleep"))
        {
            return new NettruyenCrawler(_htmlParser);
        }

        // Default: thử NettruyenCrawler
        return new NettruyenCrawler(_htmlParser);
    }

    private ICrawler? CreateCrawlerInstance(Type crawlerType)
    {
        try
        {
            // Tìm constructor nhận IHtmlParser là tham số đầu tiên
            var constructors = crawlerType.GetConstructors();
            
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                
                // Kiểm tra nếu constructor có IHtmlParser là tham số đầu tiên
                if (parameters.Length > 0 && parameters[0].ParameterType == typeof(IHtmlParser))
                {
                    // Tạo mảng arguments: IHtmlParser + các tham số optional (null)
                    var args = new object[parameters.Length];
                    args[0] = _htmlParser;
                    
                    // Các tham số còn lại (nếu có) đều là optional, set null
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue! : null!;
                    }
                    
                    return (ICrawler?)Activator.CreateInstance(crawlerType, args);
                }
            }

            // Thử constructor chỉ nhận IHtmlParser
            var singleParamConstructor = crawlerType.GetConstructor(new[] { typeof(IHtmlParser) });
            if (singleParamConstructor != null)
            {
                return (ICrawler?)Activator.CreateInstance(crawlerType, _htmlParser);
            }

            // Thử constructor không tham số
            var noParamConstructor = crawlerType.GetConstructor(Type.EmptyTypes);
            if (noParamConstructor != null)
            {
                return (ICrawler?)Activator.CreateInstance(crawlerType);
            }
        }
        catch (Exception ex)
        {
            // Log error nếu cần
            System.Diagnostics.Debug.WriteLine($"Error creating crawler instance: {ex.Message}");
        }

        return null;
    }
}

