using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyHighManga.Infastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AlternativeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CrawlerClassName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConfigurationJson = table.Column<string>(type: "text", nullable: true),
                    RequestsPerMinute = table.Column<int>(type: "integer", nullable: true),
                    RequestsPerHour = table.Column<int>(type: "integer", nullable: true),
                    DelayBetweenRequestsMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCrawledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mangas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AlternativeTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    YearOfRelease = table.Column<int>(type: "integer", nullable: true),
                    OriginalLanguage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Rating = table.Column<double>(type: "double precision", nullable: true),
                    RatingCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceMangaId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCrawledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mangas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mangas_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Mangas_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChapterNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChapterIndex = table.Column<int>(type: "integer", nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceChapterId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CountView = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrawlJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "text", nullable: true),
                    StartUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MaxPages = table.Column<int>(type: "integer", nullable: true),
                    MaxChapters = table.Column<int>(type: "integer", nullable: true),
                    TotalItems = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ProcessedItems = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SuccessItems = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailedItems = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawlJobs_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrawlJobs_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MangaGenres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MangaId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenreId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaGenres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MangaGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaGenres_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LocalFilePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    ImageFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDownloaded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pages_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrawlJobLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Exception = table.Column<string>(type: "text", nullable: true),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    CrawlJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AdditionalData = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlJobLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawlJobLogs_CrawlJobs_CrawlJobId",
                        column: x => x.CrawlJobId,
                        principalTable: "CrawlJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Authors_IsActive",
                table: "Authors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_Name",
                table: "Authors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ChapterIndex",
                table: "Chapters",
                column: "ChapterIndex");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CreatedAt",
                table: "Chapters",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_IsActive",
                table: "Chapters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_MangaId",
                table: "Chapters",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_MangaId_ChapterIndex",
                table: "Chapters",
                columns: new[] { "MangaId", "ChapterIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_MangaId_SourceChapterId",
                table: "Chapters",
                columns: new[] { "MangaId", "SourceChapterId" },
                unique: true,
                filter: "\"SourceChapterId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_PublishedAt",
                table: "Chapters",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SourceChapterId",
                table: "Chapters",
                column: "SourceChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobLogs_CrawlJobId",
                table: "CrawlJobLogs",
                column: "CrawlJobId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobLogs_CrawlJobId_CreatedAt",
                table: "CrawlJobLogs",
                columns: new[] { "CrawlJobId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobLogs_CreatedAt",
                table: "CrawlJobLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobLogs_Level",
                table: "CrawlJobLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_CreatedAt",
                table: "CrawlJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_MangaId",
                table: "CrawlJobs",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_ScheduledAt",
                table: "CrawlJobs",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_SourceId",
                table: "CrawlJobs",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_StartedAt",
                table: "CrawlJobs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_Status",
                table: "CrawlJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_Type",
                table: "CrawlJobs",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_IsActive",
                table: "Genres",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Slug",
                table: "Genres",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MangaGenres_GenreId",
                table: "MangaGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaGenres_MangaId",
                table: "MangaGenres",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaGenres_MangaId_GenreId",
                table: "MangaGenres",
                columns: new[] { "MangaId", "GenreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_AuthorId",
                table: "Mangas",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_CreatedAt",
                table: "Mangas",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_IsActive",
                table: "Mangas",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_SourceId",
                table: "Mangas",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_SourceId_SourceMangaId",
                table: "Mangas",
                columns: new[] { "SourceId", "SourceMangaId" },
                unique: true,
                filter: "\"SourceMangaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_SourceMangaId",
                table: "Mangas",
                column: "SourceMangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_Status",
                table: "Mangas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_Title",
                table: "Mangas",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ChapterId",
                table: "Pages",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ChapterId_PageNumber",
                table: "Pages",
                columns: new[] { "ChapterId", "PageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_CreatedAt",
                table: "Pages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_IsActive",
                table: "Pages",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_IsDownloaded",
                table: "Pages",
                column: "IsDownloaded");

            migrationBuilder.CreateIndex(
                name: "IX_Sources_BaseUrl",
                table: "Sources",
                column: "BaseUrl");

            migrationBuilder.CreateIndex(
                name: "IX_Sources_IsActive",
                table: "Sources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Sources_Name",
                table: "Sources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sources_Type",
                table: "Sources",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrawlJobLogs");

            migrationBuilder.DropTable(
                name: "MangaGenres");

            migrationBuilder.DropTable(
                name: "Pages");

            migrationBuilder.DropTable(
                name: "CrawlJobs");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Mangas");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Sources");
        }
    }
}
