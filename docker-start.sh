#!/bin/bash

# Script để start Docker services cho SkyHighManga

echo "🚀 Starting SkyHighManga Docker services..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker Desktop first."
    exit 1
fi

# Start services (PostgreSQL + RabbitMQ)
echo "📦 Starting PostgreSQL and RabbitMQ..."
docker-compose up -d postgres rabbitmq

# Wait for services to be healthy
echo "⏳ Waiting for services to be ready..."
sleep 5

# Check if services are running
if docker-compose ps | grep -q "skyhighmanga-postgres.*Up"; then
    echo "✅ PostgreSQL is running on localhost:5432"
else
    echo "❌ PostgreSQL failed to start"
    exit 1
fi

if docker-compose ps | grep -q "skyhighmanga-rabbitmq.*Up"; then
    echo "✅ RabbitMQ is running:"
    echo "   - AMQP: localhost:5672"
    echo "   - Management UI: http://localhost:15672"
    echo "   - Username: skyhighmanga"
    echo "   - Password: skyhighmanga123"
else
    echo "❌ RabbitMQ failed to start"
    exit 1
fi

echo ""
echo "🎉 Services are ready!"
echo ""
echo "📝 Next steps:"
echo "   1. Run your API locally (dotnet run --project SkyHighManga.Api)"
echo "   2. Or uncomment API service in docker-compose.yml to run in Docker"
echo ""
echo "📊 View logs: docker-compose logs -f"
echo "🛑 Stop services: docker-compose down"

