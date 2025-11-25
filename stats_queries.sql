-- ============================================
-- CÁC CÂU LỆNH SQL ĐỂ ĐẾM MANGA, CHAPTER, PAGE
-- ============================================

-- 1. Đếm tổng số Manga (bao gồm cả đã xóa)
SELECT COUNT(*) AS TotalMangas
FROM "Mangas";

-- 2. Đếm số Manga đang hoạt động (IsActive = true)
SELECT COUNT(*) AS ActiveMangas
FROM "Mangas"
WHERE "IsActive" = true;

-- 3. Đếm tổng số Chapter (bao gồm cả đã xóa)
SELECT COUNT(*) AS TotalChapters
FROM "Chapters";

-- 4. Đếm số Chapter đang hoạt động (IsActive = true)
SELECT COUNT(*) AS ActiveChapters
FROM "Chapters"
WHERE "IsActive" = true;

-- 5. Đếm tổng số Page (bao gồm cả đã xóa)
SELECT COUNT(*) AS TotalPages
FROM "Pages";

-- 6. Đếm số Page đang hoạt động (IsActive = true)
SELECT COUNT(*) AS ActivePages
FROM "Pages"
WHERE "IsActive" = true;

-- 7. Đếm số Page đã download (IsDownloaded = true)
SELECT COUNT(*) AS DownloadedPages
FROM "Pages"
WHERE "IsDownloaded" = true AND "IsActive" = true;

-- ============================================
-- CÂU LỆNH TỔNG HỢP - HIỂN THỊ TẤT CẢ THỐNG KÊ
-- ============================================

-- 8. Thống kê tổng hợp tất cả
SELECT 
    (SELECT COUNT(*) FROM "Mangas" WHERE "IsActive" = true) AS ActiveMangas,
    (SELECT COUNT(*) FROM "Mangas") AS TotalMangas,
    (SELECT COUNT(*) FROM "Chapters" WHERE "IsActive" = true) AS ActiveChapters,
    (SELECT COUNT(*) FROM "Chapters") AS TotalChapters,
    (SELECT COUNT(*) FROM "Pages" WHERE "IsActive" = true) AS ActivePages,
    (SELECT COUNT(*) FROM "Pages") AS TotalPages,
    (SELECT COUNT(*) FROM "Pages" WHERE "IsDownloaded" = true AND "IsActive" = true) AS DownloadedPages;

-- ============================================
-- THỐNG KÊ CHI TIẾT THEO MANGA
-- ============================================

-- 9. Đếm số Chapter và Page theo từng Manga
SELECT 
    m."Id",
    m."Title",
    COUNT(DISTINCT c."Id") AS ChapterCount,
    COUNT(DISTINCT p."Id") AS PageCount
FROM "Mangas" m
LEFT JOIN "Chapters" c ON m."Id" = c."MangaId" AND c."IsActive" = true
LEFT JOIN "Pages" p ON c."Id" = p."ChapterId" AND p."IsActive" = true
WHERE m."IsActive" = true
GROUP BY m."Id", m."Title"
ORDER BY ChapterCount DESC, PageCount DESC;

-- 10. Top 10 Manga có nhiều Chapter nhất
SELECT 
    m."Id",
    m."Title",
    COUNT(DISTINCT c."Id") AS ChapterCount
FROM "Mangas" m
LEFT JOIN "Chapters" c ON m."Id" = c."MangaId" AND c."IsActive" = true
WHERE m."IsActive" = true
GROUP BY m."Id", m."Title"
ORDER BY ChapterCount DESC
LIMIT 10;

-- 11. Top 10 Manga có nhiều Page nhất
SELECT 
    m."Id",
    m."Title",
    COUNT(DISTINCT p."Id") AS PageCount
FROM "Mangas" m
LEFT JOIN "Chapters" c ON m."Id" = c."MangaId" AND c."IsActive" = true
LEFT JOIN "Pages" p ON c."Id" = p."ChapterId" AND p."IsActive" = true
WHERE m."IsActive" = true
GROUP BY m."Id", m."Title"
ORDER BY PageCount DESC
LIMIT 10;

-- ============================================
-- THỐNG KÊ THEO TRẠNG THÁI
-- ============================================

-- 12. Đếm Manga theo trạng thái (Status)
SELECT 
    CASE 
        WHEN "Status" = 0 THEN 'Unknown'
        WHEN "Status" = 1 THEN 'Ongoing'
        WHEN "Status" = 2 THEN 'Completed'
        WHEN "Status" = 3 THEN 'OnHold'
        WHEN "Status" = 4 THEN 'Cancelled'
        ELSE 'Unknown'
    END AS StatusName,
    COUNT(*) AS Count
FROM "Mangas"
WHERE "IsActive" = true
GROUP BY "Status"
ORDER BY Count DESC;

-- ============================================
-- THỐNG KÊ THEO THỜI GIAN
-- ============================================

-- 13. Đếm Manga được tạo theo tháng
SELECT 
    DATE_TRUNC('month', "CreatedAt") AS Month,
    COUNT(*) AS MangaCount
FROM "Mangas"
WHERE "IsActive" = true
GROUP BY DATE_TRUNC('month', "CreatedAt")
ORDER BY Month DESC;

-- 14. Đếm Chapter được tạo theo tháng
SELECT 
    DATE_TRUNC('month', "CreatedAt") AS Month,
    COUNT(*) AS ChapterCount
FROM "Chapters"
WHERE "IsActive" = true
GROUP BY DATE_TRUNC('month', "CreatedAt")
ORDER BY Month DESC;

-- ============================================
-- THỐNG KÊ TỔNG QUAN VỀ DOWNLOAD
-- ============================================

-- 15. Tỷ lệ Page đã download
SELECT 
    COUNT(*) FILTER (WHERE "IsDownloaded" = true AND "IsActive" = true) AS DownloadedCount,
    COUNT(*) FILTER (WHERE "IsActive" = true) AS TotalActivePages,
    ROUND(
        COUNT(*) FILTER (WHERE "IsDownloaded" = true AND "IsActive" = true) * 100.0 / 
        NULLIF(COUNT(*) FILTER (WHERE "IsActive" = true), 0), 
        2
    ) AS DownloadPercentage
FROM "Pages";

