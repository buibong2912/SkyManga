import { Suspense } from 'react';
import MangaCard from '@/components/MangaCard';
import { searchMangas } from '@/lib/api';
import { Manga } from '@/types/manga';

interface SearchPageProps {
  searchParams: {
    q?: string;
  };
}

async function SearchResults({ query }: { query: string }) {
  const mangas = await searchMangas(query);

  if (mangas.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-600 dark:text-gray-400 text-lg">
          Không tìm thấy kết quả cho &quot;{query}&quot;
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
      {mangas.map((manga: Manga) => (
        <MangaCard key={manga.id} manga={manga} />
      ))}
    </div>
  );
}

export default function SearchPage({ searchParams }: SearchPageProps) {
  const query = searchParams.q || '';

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
        {query ? `Kết quả tìm kiếm: "${query}"` : 'Tìm kiếm truyện'}
      </h1>

      {query ? (
        <Suspense
          fallback={
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
          }
        >
          <SearchResults query={query} />
        </Suspense>
      ) : (
        <div className="text-center py-12">
          <p className="text-gray-600 dark:text-gray-400 text-lg">
            Nhập từ khóa để tìm kiếm truyện
          </p>
        </div>
      )}
    </div>
  );
}

