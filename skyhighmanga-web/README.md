# SkyHigh Manga Web

Ứng dụng web đọc truyện tranh được xây dựng với Next.js 14, TypeScript và Tailwind CSS.

## Tính năng

- 🏠 **Trang chủ**: Hiển thị danh sách truyện nổi bật và mới cập nhật
- 📚 **Chi tiết truyện**: Xem thông tin chi tiết, mô tả, thể loại và danh sách chương
- 📖 **Đọc truyện**: Trang đọc chapter với hình ảnh chất lượng cao
- 🔍 **Tìm kiếm**: Tìm kiếm truyện theo tên
- 🎨 **UI/UX hiện đại**: Giao diện đẹp, responsive, hỗ trợ dark mode
- ⚡ **Performance**: Server-side rendering với Next.js App Router

## Công nghệ sử dụng

- **Next.js 14**: React framework với App Router
- **TypeScript**: Type safety
- **Tailwind CSS**: Utility-first CSS framework
- **Next/Image**: Tối ưu hóa hình ảnh

## Cài đặt

1. Cài đặt dependencies:
```bash
npm install
```

2. Tạo file `.env.local` từ `.env.local.example`:
```bash
cp .env.local.example .env.local
```

3. Cấu hình API URL trong `.env.local`:
```
# Sử dụng HTTP endpoint (khuyến nghị cho development)
NEXT_PUBLIC_API_URL=http://localhost:5178/api

# Hoặc nếu API chạy HTTPS (sẽ tự động bỏ qua SSL verification trong dev)
# NEXT_PUBLIC_API_URL=https://localhost:7153/api
```

4. Chạy development server:
```bash
npm run dev
```

5. Mở [http://localhost:3000](http://localhost:3000) trong trình duyệt.

## Cấu trúc thư mục

```
skyhighmanga-web/
├── app/                    # Next.js App Router
│   ├── page.tsx           # Trang chủ
│   ├── layout.tsx         # Root layout
│   ├── manga/[id]/        # Trang chi tiết manga
│   ├── chapter/[id]/      # Trang đọc chapter
│   └── search/             # Trang tìm kiếm
├── components/             # React components
│   ├── Navigation.tsx     # Navigation bar
│   └── MangaCard.tsx      # Card hiển thị manga
├── lib/                    # Utilities
│   └── api.ts             # API client functions
├── types/                  # TypeScript types
│   └── manga.ts           # Manga types và interfaces
└── public/                 # Static files
```

## Kết nối với Backend API

Ứng dụng này được thiết kế để kết nối với .NET API backend (`SkyHighManga.Api`). 

### API Endpoints cần thiết:

- `GET /api/manga` - Lấy danh sách manga (pagination)
- `GET /api/manga/{id}` - Lấy chi tiết manga
- `GET /api/manga/{id}/chapters` - Lấy danh sách chapters
- `GET /api/chapter/{id}/pages` - Lấy danh sách pages
- `GET /api/manga/search?q={query}` - Tìm kiếm manga

## Build cho production

```bash
npm run build
npm start
```

## License

MIT
