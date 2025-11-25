# Flow Crawl Tối Ưu với MassTransit và Hangfire

## Tổng Quan

Hệ thống crawl được tối ưu sử dụng **MassTransit** (message queue) và **Hangfire** (background jobs) để đạt tốc độ crawl nhanh nhất có thể.

## Kiến Trúc

```
┌─────────────┐
│  Hangfire   │ ──> Trigger crawl job
└──────┬──────┘
       │
       ▼
┌─────────────────┐
│ MassTransit     │ ──> Message Queue (RabbitMQ)
│ Orchestrator    │
└──────┬──────────┘
       │
       ├──> CrawlMangaListCommand ──> Crawl danh sách manga
       │
       ├──> CrawlMangaCommand ──> Crawl manga details + chapters (20 concurrent)
       │
       ├──> CrawlChapterCommand ──> Crawl chapter (50 concurrent)
       │
       └──> CrawlPageCommand ──> Crawl pages (100 concurrent)
```

## Flow Chi Tiết

### 1. Trigger Crawl Job

**Cách 1: API Endpoint (Async - Khuyến nghị)**
```http
POST /api/crawljob/crawl-all-mangas-async?sourceName=Nettruyen&maxPages=100
```
- Sử dụng MassTransit để crawl song song ngay lập tức
- Nhanh nhất, không cần chờ

**Cách 2: Hangfire Background Job**
```http
POST /api/crawljob/crawl-all-mangas-background?sourceName=Nettruyen&maxPages=100
```
- Queue vào Hangfire, có thể schedule
- Kiểm tra tiến độ tại `/hangfire`

**Cách 3: Schedule Recurring Job**
```csharp
CrawlJobs.ScheduleRecurringCrawl(
    "daily-crawl-nettruyen",
    sourceId,
    "0 2 * * *", // 2 AM mỗi ngày
    maxPages: null
);
```

### 2. MassTransit Message Flow

#### Step 1: CrawlMangaListCommand
- **Consumer**: `CrawlMangaListConsumer`
- **Concurrency**: 5 messages đồng thời
- **Chức năng**: 
  - Crawl danh sách manga từ search pages
  - Publish `CrawlMangaCommand` cho từng manga tìm được

#### Step 2: CrawlMangaCommand
- **Consumer**: `CrawlMangaConsumer`
- **Concurrency**: 20 messages đồng thời
- **Chức năng**:
  - Crawl manga details
  - Crawl danh sách chapters
  - Lưu manga vào database
  - Publish `CrawlChapterCommand` cho từng chapter

#### Step 3: CrawlChapterCommand
- **Consumer**: `CrawlChapterConsumer`
- **Concurrency**: 50 messages đồng thời
- **Chức năng**:
  - Lưu chapter vào database
  - Publish `CrawlPageCommand` để crawl pages

#### Step 4: CrawlPageCommand
- **Consumer**: `CrawlPageConsumer`
- **Concurrency**: 100 messages đồng thời
- **Chức năng**:
  - Crawl page URLs từ chapter
  - Lưu pages vào database (đảm bảo thứ tự trong từng chapter)

## Tối Ưu Hiệu Năng

### 1. Concurrency Levels
- **Manga List**: 5 concurrent (để tránh quá tải khi crawl nhiều pages)
- **Manga Details**: 20 concurrent (crawl 20 mangas đồng thời)
- **Chapters**: 50 concurrent (crawl 50 chapters đồng thời)
- **Pages**: 100 concurrent (crawl 100 chapters/pages đồng thời)

### 2. Message Queue Benefits
- **Parallel Processing**: Tất cả messages được xử lý song song
- **Scalability**: Có thể scale bằng cách thêm nhiều workers
- **Reliability**: Messages được persist, không bị mất nếu worker crash
- **Retry**: Tự động retry nếu có lỗi

### 3. Database Optimization
- **Thứ tự pages**: Đảm bảo trong từng chapter (PageNumber = index + 1)
- **Skip existing**: Tự động skip manga/chapter/page đã tồn tại
- **Upsert logic**: Update nếu đã tồn tại, insert nếu chưa có

## Cấu Hình

### RabbitMQ
Cấu hình trong `appsettings.json`:
```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

### Hangfire
- **Storage**: PostgreSQL (cùng database với app)
- **Dashboard**: `/hangfire`
- **Workers**: `Environment.ProcessorCount * 5`

## Monitoring

### 1. Hangfire Dashboard
- Truy cập: `http://localhost:5000/hangfire`
- Xem: Jobs, Recurring jobs, Failed jobs, Retries

### 2. Crawl Job Status
```http
GET /api/crawljob/jobs/{crawlJobId}
```
- Xem chi tiết crawl job
- Xem logs
- Xem progress (processedItems, successItems, failedItems)

### 3. MassTransit Monitoring
- RabbitMQ Management UI: `http://localhost:15672`
- Xem queues, messages, consumers

## So Sánh Tốc Độ

### Trước (Synchronous)
- Crawl tuần tự: 1 manga → 1 chapter → 1 page
- Thời gian: ~10-15 phút cho 100 mangas

### Sau (MassTransit + Hangfire)
- Crawl song song: 20 mangas + 50 chapters + 100 pages đồng thời
- Thời gian: ~2-3 phút cho 100 mangas
- **Tăng tốc: 5-7x**

## Best Practices

1. **Sử dụng Async endpoint** cho crawl nhanh nhất
2. **Schedule recurring jobs** cho crawl tự động hàng ngày
3. **Monitor Hangfire dashboard** để theo dõi jobs
4. **Kiểm tra RabbitMQ** nếu có vấn đề về message queue
5. **Tăng concurrency** nếu server có nhiều resources

## Troubleshooting

### Messages không được xử lý
- Kiểm tra RabbitMQ đang chạy
- Kiểm tra connection string trong appsettings.json
- Kiểm tra logs trong Hangfire dashboard

### Crawl chậm
- Tăng PrefetchCount trong DependencyInjection.cs
- Tăng số workers trong Hangfire
- Kiểm tra database connection pool

### Duplicate key errors
- Đã được fix trong SavePagesAsync
- Kiểm tra unique constraints trong database

