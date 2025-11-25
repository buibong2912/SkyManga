#!/bin/bash

# Script để xem logs của các services

SERVICE=${1:-api}

case $SERVICE in
    api)
        echo "📊 Viewing API logs (Ctrl+C to exit)..."
        docker logs -f skyhighmanga-api
        ;;
    rabbitmq)
        echo "📊 Viewing RabbitMQ logs (Ctrl+C to exit)..."
        docker logs -f skyhighmanga-rabbitmq
        ;;
    postgres)
        echo "📊 Viewing PostgreSQL logs (Ctrl+C to exit)..."
        docker logs -f skyhighmanga-postgres
        ;;
    all)
        echo "📊 Viewing all services logs (Ctrl+C to exit)..."
        docker compose logs -f
        ;;
    *)
        echo "Usage: $0 [api|rabbitmq|postgres|all]"
        echo "Default: api"
        exit 1
        ;;
esac

