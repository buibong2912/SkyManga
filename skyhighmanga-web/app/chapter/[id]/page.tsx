import Image from 'next/image';
import Link from 'next/link';
import { fetchChapterPages } from '@/lib/api';
import { Page } from '@/types/manga';
import { notFound } from 'next/navigation';

interface PageProps {
  params: Promise<{
    id: string;
  }>;
}

export default async function ChapterPage({ params }: PageProps) {
  const { id } = await params;
  const pages = await fetchChapterPages(id);

  if (!pages || pages.length === 0) {
    notFound();
  }

  // Sort pages by pageNumber
  const sortedPages = [...pages].sort((a: Page, b: Page) => a.pageNumber - b.pageNumber);

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Navigation Bar */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4 mb-6 sticky top-16 z-40">
        <div className="flex items-center justify-between">
          <Link
            href="/"
            className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors"
          >
            ← Về trang chủ
          </Link>
          <div className="text-sm text-gray-600 dark:text-gray-400">
            Trang 1 / {sortedPages.length}
          </div>
          <div className="flex gap-2">
            <button className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors">
              ⬅ Trước
            </button>
            <button className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors">
              Sau ➡
            </button>
          </div>
        </div>
      </div>

      {/* Reading Mode Options */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4 mb-6">
        <div className="flex items-center justify-center gap-4">
          <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
            <input type="checkbox" className="rounded" />
            Chế độ dọc
          </label>
          <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
            <input type="checkbox" className="rounded" />
            Tự động cuộn
          </label>
        </div>
      </div>

      {/* Pages */}
      <div className="space-y-4">
        {sortedPages.map((page: Page, index: number) => (
          <div
            key={page.id}
            className="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden"
          >
            <div className="relative w-full" style={{ minHeight: '800px' }}>
              <Image
                src={page.imageUrl}
                alt={`Trang ${page.pageNumber}`}
                fill
                className="object-contain"
                sizes="(max-width: 768px) 100vw, 80vw"
                priority={index < 3}
              />
            </div>
            <div className="p-2 text-center text-sm text-gray-600 dark:text-gray-400">
              Trang {page.pageNumber} / {sortedPages.length}
            </div>
          </div>
        ))}
      </div>

      {/* Bottom Navigation */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4 mt-6">
        <div className="flex items-center justify-between">
          <button className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors">
            ⬅ Chương trước
          </button>
          <Link
            href="/"
            className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors"
          >
            Về danh sách chương
          </Link>
          <button className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors">
            Chương sau ➡
          </button>
        </div>
      </div>
    </div>
  );
}

