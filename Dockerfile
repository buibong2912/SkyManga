# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY SkyHighManga.sln ./

# Copy project files để restore dependencies
COPY SkyHighManga.Api/*.csproj ./SkyHighManga.Api/
COPY SkyHighManga.Application/*.csproj ./SkyHighManga.Application/
COPY SkyHighManga.Domain/*.csproj ./SkyHighManga.Domain/
COPY SkyHighManga.Infastructure/*.csproj ./SkyHighManga.Infastructure/

# Restore dependencies
RUN dotnet restore SkyHighManga.sln

# Copy all source files
COPY SkyHighManga.Api/ ./SkyHighManga.Api/
COPY SkyHighManga.Application/ ./SkyHighManga.Application/
COPY SkyHighManga.Domain/ ./SkyHighManga.Domain/
COPY SkyHighManga.Infastructure/ ./SkyHighManga.Infastructure/

# Build the application
WORKDIR /src/SkyHighManga.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/swagger/index.html || exit 1

# Run the application
ENTRYPOINT ["dotnet", "SkyHighManga.Api.dll"]

