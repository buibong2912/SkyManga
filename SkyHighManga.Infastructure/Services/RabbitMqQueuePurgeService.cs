using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SkyHighManga.Infastructure.Services;

/// <summary>
/// Service để purge tất cả queues trong RabbitMQ khi khởi động lại ứng dụng
/// Tránh xử lý lại các messages cũ
/// </summary>
public interface IRabbitMqQueuePurgeService
{
    /// <summary>
    /// Purge tất cả queues liên quan đến crawl
    /// </summary>
    Task PurgeAllQueuesAsync(CancellationToken cancellationToken = default);
}

public class RabbitMqQueuePurgeService : IRabbitMqQueuePurgeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqQueuePurgeService> _logger;

    public RabbitMqQueuePurgeService(
        IConfiguration configuration,
        ILogger<RabbitMqQueuePurgeService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PurgeAllQueuesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var rabbitMqHost = _configuration["RabbitMQ:Host"] ?? "localhost";
            var rabbitMqPortStr = _configuration["RabbitMQ:Port"];
            var rabbitMqPort = 5672;
            if (!string.IsNullOrEmpty(rabbitMqPortStr) && int.TryParse(rabbitMqPortStr, out var parsedPort))
            {
                rabbitMqPort = parsedPort;
            }
            var rabbitMqUsername = _configuration["RabbitMQ:Username"] ?? "guest";
            var rabbitMqPassword = _configuration["RabbitMQ:Password"] ?? "guest";

            _logger.LogInformation("Bắt đầu purge tất cả queues trong RabbitMQ...");

            // Tạo connection factory
            var factory = new ConnectionFactory
            {
                HostName = rabbitMqHost,
                Port = rabbitMqPort,
                UserName = rabbitMqUsername,
                Password = rabbitMqPassword,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            // Danh sách queues cần purge
            var queuesToPurge = new[]
            {
                "crawl-manga-list",
                "crawl-all-mangas-chapters",
                "crawl-manga",
                "crawl-chapter",
                "crawl-page"
            };

            // Retry logic: Thử kết nối đến RabbitMQ với retry
            const int maxRetries = 3;
            const int retryDelaySeconds = 2;
            
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        using var connection = factory.CreateConnection();
                        using var channel = connection.CreateModel();

                        var purgedCount = 0;
                        foreach (var queueName in queuesToPurge)
                        {
                            try
                            {
                                // Kiểm tra queue có tồn tại không
                                var queueDeclareResult = channel.QueueDeclarePassive(queueName);
                                var messageCount = (int)queueDeclareResult.MessageCount;

                                if (messageCount > 0)
                                {
                                    // Purge queue
                                    channel.QueuePurge(queueName);
                                    _logger.LogInformation("✅ Đã purge queue '{QueueName}': {MessageCount} messages", 
                                        queueName, messageCount);
                                    purgedCount += messageCount;
                                }
                                else
                                {
                                    _logger.LogInformation("Queue '{QueueName}' đã trống", queueName);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Queue có thể chưa tồn tại, bỏ qua
                                _logger.LogDebug("Queue '{QueueName}' chưa tồn tại hoặc không thể purge: {Error}", 
                                    queueName, ex.Message);
                            }
                        }

                        _logger.LogInformation("✅ Hoàn thành purge queues: Tổng cộng {TotalMessages} messages đã bị xóa", 
                            purgedCount);
                    }, cancellationToken);
                    
                    // Thành công, thoát khỏi retry loop
                    return;
                }
                catch (Exception ex)
                {
                    if (retry < maxRetries - 1)
                    {
                        _logger.LogWarning("Lần thử {Retry}/{MaxRetries} thất bại khi kết nối đến RabbitMQ. " +
                            "Sẽ thử lại sau {Delay} giây...", retry + 1, maxRetries, retryDelaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
                    }
                    else
                    {
                        _logger.LogError(ex, "Không thể kết nối đến RabbitMQ sau {MaxRetries} lần thử. " +
                            "Có thể RabbitMQ chưa sẵn sàng hoặc chưa khởi động. " +
                            "Ứng dụng sẽ tiếp tục chạy nhưng có thể xử lý messages cũ.", maxRetries);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi purge queues trong RabbitMQ");
            // Không throw exception để ứng dụng vẫn có thể khởi động
        }
    }
}

