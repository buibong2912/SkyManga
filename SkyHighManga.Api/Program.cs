using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using SkyHighManga.Infastructure;
using SkyHighManga.Infastructure.Data;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Đăng ký MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.Load("SkyHighManga.Application")));

// Đăng ký tất cả Infrastructure services (DbContext, Repositories, Services, Crawlers)
builder.Services.AddInfrastructure(builder.Configuration);

// Đăng ký Hangfire
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire"
    });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 5; // Tối ưu số workers
    options.Queues = new[] { "default", "crawl" }; // Custom queues
});

builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await DbInitializer.SeedAsync(context);
        Console.WriteLine("Database seeded successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding database: {ex.Message}");
    }
}

// Purge tất cả queues trong RabbitMQ khi khởi động lại
// Tránh xử lý lại các messages cũ
using (var scope = app.Services.CreateScope())
{
    var purgeService = scope.ServiceProvider.GetRequiredService<SkyHighManga.Infastructure.Services.IRabbitMqQueuePurgeService>();
    try
    {
        await purgeService.PurgeAllQueuesAsync();
        Console.WriteLine("RabbitMQ queues purged successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not purge RabbitMQ queues: {ex.Message}");
        Console.WriteLine("Application will continue, but old messages may be processed.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyHighManga API V1");
        c.RoutePrefix = "swagger"; // Swagger UI available at /swagger
    });
}

app.UseHttpsRedirection();

// Cấu hình Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() } // Cho phép tất cả trong dev
});

app.UseAuthorization();

app.MapControllers();

app.Run();

// Simple authorization filter cho Hangfire (trong production nên thêm authentication)
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Trong production, thêm authentication logic ở đây
        return true;
    }
}