import Link from 'next/link';
import Image from 'next/image';
import { Manga } from '@/types/manga';

interface MangaCardProps {
  manga: Manga;
}

export default function MangaCard({ manga }: MangaCardProps) {
  const statusColors = {
    Ongoing: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
    Completed: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300',
    OnHold: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300',
    Cancelled: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300',
    Unknown: 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300',
  };

  return (
    <Link href={`/manga/${manga.id}`} className="group">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md hover:shadow-xl transition-all duration-300 overflow-hidden transform hover:-translate-y-1">
        {/* Cover Image */}
        <div className="relative aspect-[3/4] overflow-hidden bg-gray-200 dark:bg-gray-700">
          {manga.coverImageUrl ? (
            <Image
              src={manga.coverImageUrl}
              alt={manga.title}
              fill
              className="object-cover group-hover:scale-105 transition-transform duration-300"
              sizes="(max-width: 768px) 50vw, (max-width: 1200px) 33vw, 25vw"
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center">
              <span className="text-gray-400 text-4xl">📚</span>
            </div>
          )}
          
          {/* Status Badge */}
          <div className="absolute top-2 right-2">
            <span
              className={`px-2 py-1 text-xs font-semibold rounded ${
                statusColors[manga.status] || statusColors.Unknown
              }`}
            >
              {manga.status === 'Ongoing' ? 'Đang ra' : 
               manga.status === 'Completed' ? 'Hoàn thành' :
               manga.status === 'OnHold' ? 'Tạm dừng' :
               manga.status === 'Cancelled' ? 'Hủy' : 'Không xác định'}
            </span>
          </div>
        </div>

        {/* Manga Info */}
        <div className="p-4">
          <h3 className="font-semibold text-lg text-gray-900 dark:text-white line-clamp-2 group-hover:text-purple-600 dark:group-hover:text-purple-400 transition-colors">
            {manga.title}
          </h3>
          
          {manga.author && (
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
              {manga.author.name}
            </p>
          )}

          {manga.genres && manga.genres.length > 0 && (
            <div className="flex flex-wrap gap-1 mt-2">
              {manga.genres.slice(0, 3).map((genre) => (
                <span
                  key={genre.id}
                  className="px-2 py-0.5 text-xs bg-purple-100 dark:bg-purple-900 text-purple-700 dark:text-purple-300 rounded"
                >
                  {genre.name}
                </span>
              ))}
              {manga.genres.length > 3 && (
                <span className="px-2 py-0.5 text-xs text-gray-500 dark:text-gray-400">
                  +{manga.genres.length - 3}
                </span>
              )}
            </div>
          )}

          {manga.rating && (
            <div className="flex items-center mt-2 text-sm text-gray-600 dark:text-gray-400">
              <span className="text-yellow-500">⭐</span>
              <span className="ml-1">{manga.rating.toFixed(1)}</span>
              {manga.viewCount && (
                <span className="ml-4">👁 {manga.viewCount.toLocaleString()}</span>
              )}
            </div>
          )}
        </div>
      </div>
    </Link>
  );
}

