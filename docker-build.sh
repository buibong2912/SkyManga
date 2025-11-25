#!/bin/bash

# Script để build và start Docker services cho SkyHighManga

echo "🔨 Building Docker images (no cache)..."

# Build API image
docker compose build --no-cache api

if [ $? -eq 0 ]; then
    echo "✅ Build thành công!"
    echo ""
    echo "🚀 Starting services..."
    docker compose up -d
    
    if [ $? -eq 0 ]; then
        echo "✅ Services đã được start!"
        echo ""
        echo "📊 Viewing API logs (Ctrl+C to exit)..."
        echo ""
        docker logs -f skyhighmanga-api
    else
        echo "❌ Lỗi khi start services"
        exit 1
    fi
else
    echo "❌ Lỗi khi build image"
    exit 1
fi

