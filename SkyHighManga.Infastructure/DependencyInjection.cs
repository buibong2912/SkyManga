using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkyHighManga.Application.Interfaces.Crawlers;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Infastructure.Consumers;
using SkyHighManga.Infastructure.Crawlers;
using SkyHighManga.Infastructure.Data;
using SkyHighManga.Infastructure.Jobs;
using SkyHighManga.Infastructure.Repositories;
using SkyHighManga.Infastructure.Services;

namespace SkyHighManga.Infastructure;

/// <summary>
/// Extension methods để đăng ký services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký tất cả Infrastructure services
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Đăng ký DbContext với PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Đăng ký UnitOfWork (Scoped - mỗi request một instance)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Đăng ký Repositories (Scoped)
        services.AddScoped<IMangaRepository, MangaRepository>();
        services.AddScoped<IChapterRepository, ChapterRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IGenreRepository, GenreRepository>();
        services.AddScoped<ICrawlJobRepository, CrawlJobRepository>();

        // Đăng ký Services (Scoped)
        services.AddScoped<IMangaService, MangaService>();
        services.AddScoped<ICrawlJobService, CrawlJobService>();
        services.AddScoped<ICrawlJobOrchestratorService, CrawlJobOrchestratorService>();
        
        // Đăng ký Hangfire Jobs (Scoped để có thể inject dependencies)
        services.AddScoped<CrawlJobs>();

        // Đăng ký HTML Parser (Singleton - có thể dùng chung)
        services.AddSingleton<IHtmlParser, HtmlAgilityPackParser>();

        // Đăng ký Crawler Factory (Singleton)
        services.AddSingleton<ICrawlerFactory>(serviceProvider =>
        {
            var htmlParser = serviceProvider.GetRequiredService<IHtmlParser>();
            var factory = new CrawlerFactory(htmlParser);
            
            // Đăng ký các crawlers
            factory.RegisterCrawler<NettruyenCrawler>("NettruyenCrawler");
            
            return factory;
        });

        // Đăng ký MassTransit với RabbitMQ
        var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMqPortStr = configuration["RabbitMQ:Port"];
        var rabbitMqPort = 5672; // Default port
        if (!string.IsNullOrEmpty(rabbitMqPortStr) && int.TryParse(rabbitMqPortStr, out var parsedPort))
        {
            rabbitMqPort = parsedPort;
        }
        
        // Validate port range
        if (rabbitMqPort < 1 || rabbitMqPort > 65535)
        {
            throw new ArgumentException($"Invalid RabbitMQ port: {rabbitMqPort}. Port must be between 1 and 65535.");
        }
        
        var rabbitMqUsername = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitMqPassword = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
            // Đăng ký consumers
            x.AddConsumer<CrawlMangaListConsumer>();
            x.AddConsumer<CrawlMangaConsumer>();
            x.AddConsumer<CrawlChapterConsumer>();
            x.AddConsumer<CrawlPageConsumer>();
            x.AddConsumer<CrawlAllMangasChaptersConsumer>();

            // Cấu hình bus với RabbitMQ
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, (ushort)rabbitMqPort, "/", h =>
                {
                    h.Username(rabbitMqUsername);
                    h.Password(rabbitMqPassword);
                });
                
                // Configure retry policy for messages
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                
                // Enable automatic recovery
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                    cb.TripThreshold = 15;
                    cb.ActiveThreshold = 10;
                    cb.ResetInterval = TimeSpan.FromMinutes(5);
                });

                // Cấu hình consumers với concurrency cao để crawl nhanh
                cfg.ReceiveEndpoint("crawl-manga-list", e =>
                {
                    e.ConfigureConsumer<CrawlMangaListConsumer>(context);
                    e.PrefetchCount = 20; // Tăng lên 20 để publish nhiều manga nhanh hơn
                    e.ConcurrentMessageLimit = 20;
                });

                cfg.ReceiveEndpoint("crawl-all-mangas-chapters", e =>
                {
                    e.ConfigureConsumer<CrawlAllMangasChaptersConsumer>(context);
                    e.PrefetchCount = 5; // Tăng lên 5 để có thể xử lý nhiều batch
                    e.ConcurrentMessageLimit = 5;
                });

                cfg.ReceiveEndpoint("crawl-manga", e =>
                {
                    e.ConfigureConsumer<CrawlMangaConsumer>(context);
                    e.PrefetchCount = 100; // Tăng lên 100 mangas đồng thời (đa luồng rất cao)
                    e.ConcurrentMessageLimit = 100; // Giới hạn concurrent messages
                });

                cfg.ReceiveEndpoint("crawl-chapter", e =>
                {
                    e.ConfigureConsumer<CrawlChapterConsumer>(context);
                    e.PrefetchCount = 500; // Tăng lên 500 chapters đồng thời (đa luồng rất cao)
                    e.ConcurrentMessageLimit = 500; // Giới hạn concurrent messages
                });

                cfg.ReceiveEndpoint("crawl-page", e =>
                {
                    e.ConfigureConsumer<CrawlPageConsumer>(context);
                    e.PrefetchCount = 1000; // Tăng lên 1000 pages đồng thời (đa luồng cực cao)
                    e.ConcurrentMessageLimit = 1000; // Giới hạn concurrent messages
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        // Đăng ký RabbitMQ Queue Purge Service
        services.AddScoped<IRabbitMqQueuePurgeService, RabbitMqQueuePurgeService>();

        return services;
    }
}

