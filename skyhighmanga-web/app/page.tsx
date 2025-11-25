import MangaCard from '@/components/MangaCard';
import { fetchMangas } from '@/lib/api';
import { Manga } from '@/types/manga';

export default async function Home() {
  // Fetch featured mangas (sorted by viewCount)
  const featuredResult = await fetchMangas(1, 12, {
    sortBy: 'ViewCount',
    sortDescending: true,
  });
  const featuredMangas = featuredResult?.data || [];

  // Fetch latest mangas (sorted by UpdatedAt)
  const latestResult = await fetchMangas(1, 12, {
    sortBy: 'UpdatedAt',
    sortDescending: true,
  });
  const latestMangas = latestResult?.data || [];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Hero Section */}
      <div className="mb-12 text-center">
        <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
          SkyHigh Manga
        </h1>
        <p className="text-xl text-gray-600 dark:text-gray-400">
          Kho truyện tranh đa dạng, cập nhật nhanh nhất
        </p>
      </div>

      {/* Featured Section */}
      <section className="mb-12">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            Truyện nổi bật
          </h2>
          <a
            href="/mangas?type=featured"
            className="text-purple-600 dark:text-purple-400 hover:text-purple-700 dark:hover:text-purple-300 font-semibold transition-colors"
          >
            Xem tất cả →
          </a>
        </div>
        {featuredMangas && featuredMangas.length > 0 ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {featuredMangas.map((manga: Manga) => (
              <MangaCard key={manga.id} manga={manga} />
            ))}
          </div>
        ) : (
          <div className="text-center py-12">
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              Chưa có truyện nào. Hãy bắt đầu crawl truyện từ API!
            </p>
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
              {[...Array(12)].map((_, i) => (
                <div
                  key={i}
                  className="bg-white dark:bg-gray-800 rounded-lg shadow-md aspect-[3/4] animate-pulse"
                >
                  <div className="w-full h-full bg-gray-200 dark:bg-gray-700"></div>
                </div>
              ))}
            </div>
          </div>
        )}
      </section>

      {/* Latest Updates */}
      <section>
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            Mới cập nhật
          </h2>
          <a
            href="/mangas?type=latest"
            className="text-purple-600 dark:text-purple-400 hover:text-purple-700 dark:hover:text-purple-300 font-semibold transition-colors"
          >
            Xem tất cả →
          </a>
        </div>
        {latestMangas && latestMangas.length > 0 ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {latestMangas.map((manga: Manga) => (
              <MangaCard key={manga.id} manga={manga} />
            ))}
          </div>
        ) : (
          <p className="text-gray-600 dark:text-gray-400 text-center py-8">
            Chưa có truyện mới cập nhật
          </p>
        )}
      </section>
    </div>
  );
}
