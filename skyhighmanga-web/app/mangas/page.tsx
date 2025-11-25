import MangaCard from '@/components/MangaCard';
import Pagination from '@/components/Pagination';
import { fetchMangas } from '@/lib/api';
import { Manga } from '@/types/manga';

interface PageProps {
  searchParams: Promise<{
    page?: string;
    sortBy?: string;
    sortDescending?: string;
    type?: string;
  }>;
}

export default async function MangasPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const currentPage = parseInt(params.page || '1', 10);
  const pageSize = 24;
  const sortBy = params.sortBy || 'UpdatedAt';
  const sortDescending = params.sortDescending !== 'false';
  const type = params.type || 'all'; // 'all', 'featured', 'latest'

  // Determine sorting based on type
  let sortOption = sortBy;
  let sortDesc = sortDescending;

  if (type === 'featured') {
    // Featured: sort by viewCount or rating
    sortOption = 'ViewCount';
    sortDesc = true;
  } else if (type === 'latest') {
    // Latest: sort by UpdatedAt
    sortOption = 'UpdatedAt';
    sortDesc = true;
  }

  const result = await fetchMangas(currentPage, pageSize, {
    sortBy: sortOption,
    sortDescending: sortDesc,
  });

  const mangas = result?.data || [];
  const total = result?.total || 0;
  const totalPages = Math.ceil(total / pageSize);

  // Get title based on type
  const getTitle = () => {
    switch (type) {
      case 'featured':
        return 'Truyện nổi bật';
      case 'latest':
        return 'Mới cập nhật';
      default:
        return 'Tất cả truyện';
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
          {getTitle()}
        </h1>
        <p className="text-gray-600 dark:text-gray-400">
          Tổng cộng {total.toLocaleString()} truyện
        </p>
      </div>

      {/* Manga Grid */}
      {mangas && mangas.length > 0 ? (
        <>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {mangas.map((manga: Manga) => (
              <MangaCard key={manga.id} manga={manga} />
            ))}
          </div>

          {/* Pagination */}
          <Pagination
            currentPage={currentPage}
            totalPages={totalPages}
            baseUrl="/mangas"
          />
        </>
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
    </div>
  );
}

