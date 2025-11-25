import Image from 'next/image';
import Link from 'next/link';
import { fetchMangaById, fetchMangaChapters } from '@/lib/api';
import { Manga, Chapter, Genre } from '@/types/manga';
import { notFound } from 'next/navigation';

interface PageProps {
  params: Promise<{
    id: string;
  }>;
}

export default async function MangaDetailPage({ params }: PageProps) {
  const { id } = await params;
  const manga = await fetchMangaById(id);
  const chapters = await fetchMangaChapters(id);

  if (!manga) {
    notFound();
  }

  const statusLabels: Record<string, string> = {
    Ongoing: 'Đang ra',
    Completed: 'Hoàn thành',
    OnHold: 'Tạm dừng',
    Cancelled: 'Hủy',
    Unknown: 'Không xác định',
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg overflow-hidden">
        {/* Manga Header */}
        <div className="md:flex">
          {/* Cover Image */}
          <div className="md:w-1/3 p-6">
            <div className="relative aspect-[3/4] rounded-lg overflow-hidden bg-gray-200 dark:bg-gray-700">
              {manga.coverImageUrl ? (
                <Image
                  src={manga.coverImageUrl}
                  alt={manga.title}
                  fill
                  className="object-cover"
                  sizes="(max-width: 768px) 100vw, 33vw"
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center">
                  <span className="text-gray-400 text-6xl">📚</span>
                </div>
              )}
            </div>
          </div>

          {/* Manga Info */}
          <div className="md:w-2/3 p-6">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">
              {manga.title}
            </h1>

            {manga.author && (
              <div className="mb-4">
                <span className="text-gray-600 dark:text-gray-400">Tác giả: </span>
                <span className="font-semibold text-gray-900 dark:text-white">
                  {manga.author.name}
                </span>
              </div>
            )}

            {manga.description && (
              <div className="mb-4">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                  Mô tả
                </h3>
                <p className="text-gray-700 dark:text-gray-300 leading-relaxed">
                  {manga.description}
                </p>
              </div>
            )}

            {/* Metadata */}
            <div className="grid grid-cols-2 gap-4 mb-4">
              <div>
                <span className="text-gray-600 dark:text-gray-400">Trạng thái: </span>
                <span className="font-semibold text-gray-900 dark:text-white">
                  {statusLabels[manga.status] || manga.status}
                </span>
              </div>
              {manga.rating && (
                <div>
                  <span className="text-gray-600 dark:text-gray-400">Đánh giá: </span>
                  <span className="font-semibold text-yellow-500">
                    ⭐ {manga.rating.toFixed(1)}
                  </span>
                </div>
              )}
              {manga.viewCount && (
                <div>
                  <span className="text-gray-600 dark:text-gray-400">Lượt xem: </span>
                  <span className="font-semibold text-gray-900 dark:text-white">
                    {manga.viewCount.toLocaleString()}
                  </span>
                </div>
              )}
            </div>

            {/* Genres */}
            {manga.genres && manga.genres.length > 0 && (
              <div className="mb-4">
                <span className="text-gray-600 dark:text-gray-400 block mb-2">Thể loại:</span>
                <div className="flex flex-wrap gap-2">
                  {manga.genres.map((genre: Genre) => (
                    <span
                      key={genre.id}
                      className="px-3 py-1 bg-purple-100 dark:bg-purple-900 text-purple-700 dark:text-purple-300 rounded-full text-sm"
                    >
                      {genre.name}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Chapters List */}
        <div className="border-t border-gray-200 dark:border-gray-700 p-6">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
            Danh sách chương ({chapters.length})
          </h2>
          {chapters.length > 0 ? (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-2">
              {chapters
                .sort((a: Chapter, b: Chapter) => {
                  if (a.chapterIndex !== undefined && b.chapterIndex !== undefined) {
                    return b.chapterIndex - a.chapterIndex;
                  }
                  return 0;
                })
                .map((chapter: Chapter) => (
                  <Link
                    key={chapter.id}
                    href={`/chapter/${chapter.id}`}
                    className="block p-3 bg-gray-50 dark:bg-gray-700 rounded-lg hover:bg-purple-100 dark:hover:bg-purple-900 transition-colors"
                  >
                    <div className="font-semibold text-gray-900 dark:text-white text-sm">
                      {chapter.chapterNumber || `Chương ${chapter.chapterIndex || ''}`}
                    </div>
                    {chapter.title && (
                      <div className="text-xs text-gray-600 dark:text-gray-400 mt-1 line-clamp-2">
                        {chapter.title}
                      </div>
                    )}
                    {chapter.publishedAt && (
                      <div className="text-xs text-gray-500 dark:text-gray-500 mt-1">
                        {new Date(chapter.publishedAt).toLocaleDateString('vi-VN')}
                      </div>
                    )}
                  </Link>
                ))}
            </div>
          ) : (
            <p className="text-gray-600 dark:text-gray-400 text-center py-8">
              Chưa có chương nào. Hãy crawl chapters từ API!
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

