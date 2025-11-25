using Microsoft.EntityFrameworkCore;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Kiểm tra xem đã có Source nào chưa
        if (await context.Sources.AnyAsync())
        {
            return; // Đã có data, không seed lại
        }

        // Seed Nettruyen Source
        var nettruyenSource = new Source
        {
            Id = Guid.NewGuid(),
            Name = "Nettruyen",
            BaseUrl = "https://aquastarsleep.co.uk",
            Description = "Nettruyen - Nguồn truyện tranh lớn",
            Type = SourceType.Website,
            IsActive = true,
            CrawlerClassName = "NettruyenCrawler",
            RequestsPerMinute = 30,
            RequestsPerHour = 1000,
            DelayBetweenRequestsMs = 2000, // 2 giây delay giữa các request
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Sources.Add(nettruyenSource);
        await context.SaveChangesAsync();
    }
}

