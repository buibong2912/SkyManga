# Docker Setup cho SkyHighManga

## Yêu cầu

- Docker Desktop (hoặc Docker Engine + Docker Compose)
- .NET 9.0 SDK (nếu chạy local)

## Cấu trúc

- `docker-compose.yml` - Định nghĩa các services (PostgreSQL, RabbitMQ, API)
- `Dockerfile` - Build và run API application
- `.dockerignore` - Files/folders không cần copy vào Docker image

## Services

### 1. PostgreSQL
- **Port**: 5432
- **Database**: sky_manga
- **Username**: skyhighmanga
- **Password**: skyhighmanga123
- **Volume**: `postgres_data` (persistent data)

### 2. RabbitMQ
- **AMQP Port**: 5672
- **Management UI**: http://localhost:15672
- **Username**: skyhighmanga
- **Password**: skyhighmanga123
- **Volume**: `rabbitmq_data` (persistent data)

### 3. API Application
- **Port**: 5000 (mapped to 8080 inside container)
- **Swagger**: http://localhost:5000/swagger
- **Hangfire Dashboard**: http://localhost:5000/hangfire

## Cách sử dụng

### Option 1: Chạy chỉ Infrastructure (PostgreSQL + RabbitMQ)

Chỉ chạy database và message broker, API chạy local:

```bash
# Start services
docker-compose up -d postgres rabbitmq

# Check status
docker-compose ps

# View logs
docker-compose logs -f rabbitmq
docker-compose logs -f postgres

# Stop services
docker-compose down
```

**Lưu ý**: Khi chạy API local, sử dụng `appsettings.Development.json` đã được cấu hình để kết nối với localhost.

### Option 2: Chạy tất cả (PostgreSQL + RabbitMQ + API)

Chạy tất cả services trong Docker:

```bash
# Build và start tất cả services
docker-compose up -d --build

# Check status
docker-compose ps

# View logs
docker-compose logs -f api
docker-compose logs -f rabbitmq
docker-compose logs -f postgres

# Stop tất cả
docker-compose down

# Stop và xóa volumes (xóa data)
docker-compose down -v
```

### Option 3: Chạy từng service riêng

```bash
# Chỉ start PostgreSQL
docker-compose up -d postgres

# Chỉ start RabbitMQ
docker-compose up -d rabbitmq

# Start API (sau khi đã có postgres và rabbitmq)
docker-compose up -d api
```

## Truy cập Services

### PostgreSQL
```bash
# Connect từ local
psql -h localhost -p 5432 -U skyhighmanga -d sky_manga
# Password: skyhighmanga123
```

### RabbitMQ Management UI
- URL: http://localhost:15672
- Username: `skyhighmanga`
- Password: `skyhighmanga123`

### API Endpoints
- Swagger: http://localhost:5000/swagger
- Hangfire Dashboard: http://localhost:5000/hangfire
- Health Check: http://localhost:5000/swagger/index.html

## Database Migrations

Khi chạy API lần đầu, migrations sẽ tự động chạy. Nếu cần chạy manual:

```bash
# Nếu API chạy trong Docker
docker-compose exec api dotnet ef database update --project SkyHighManga.Infastructure

# Nếu API chạy local
cd SkyHighManga.Api
dotnet ef database update --project ../SkyHighManga.Infastructure
```

## Troubleshooting

### Port đã được sử dụng
Nếu port 5432, 5672, 15672, hoặc 5000 đã được sử dụng, sửa trong `docker-compose.yml`:

```yaml
ports:
  - "5433:5432"  # Thay đổi port bên trái
```

### Xóa và tạo lại containers
```bash
# Stop và xóa containers
docker-compose down

# Xóa volumes (mất data)
docker-compose down -v

# Build lại và start
docker-compose up -d --build
```

### Xem logs
```bash
# Tất cả services
docker-compose logs -f

# Một service cụ thể
docker-compose logs -f api
docker-compose logs -f rabbitmq
docker-compose logs -f postgres
```

### Kiểm tra health
```bash
# Check container status
docker-compose ps

# Check health của từng service
docker inspect skyhighmanga-postgres | grep Health
docker inspect skyhighmanga-rabbitmq | grep Health
```

## Environment Variables

Có thể override environment variables trong `docker-compose.yml` hoặc tạo file `.env`:

```env
POSTGRES_PASSWORD=your_password
RABBITMQ_PASSWORD=your_password
```

## Production

Để deploy production, cần:
1. Thay đổi passwords trong `docker-compose.yml`
2. Sử dụng secrets management
3. Cấu hình SSL/TLS
4. Setup backup cho PostgreSQL
5. Monitor và logging

## Useful Commands

```bash
# Rebuild API image
docker-compose build api

# Restart một service
docker-compose restart api

# Execute command trong container
docker-compose exec api bash
docker-compose exec postgres psql -U skyhighmanga -d sky_manga

# Clean up
docker-compose down -v
docker system prune -a
```

