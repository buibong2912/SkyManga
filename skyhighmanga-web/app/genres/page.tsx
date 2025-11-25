export default function GenresPage() {
  const genres = [
    'Action', 'Adventure', 'Comedy', 'Drama', 'Fantasy',
    'Horror', 'Mystery', 'Romance', 'Sci-Fi', 'Slice of Life',
    'Sports', 'Supernatural', 'Thriller', 'Yaoi', 'Yuri'
  ];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">
        Thể loại truyện
      </h1>
      
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
        {genres.map((genre) => (
          <a
            key={genre}
            href={`/search?genre=${encodeURIComponent(genre)}`}
            className="block p-6 bg-white dark:bg-gray-800 rounded-lg shadow-md hover:shadow-xl transition-all duration-300 text-center hover:bg-purple-50 dark:hover:bg-purple-900"
          >
            <span className="text-lg font-semibold text-gray-900 dark:text-white">
              {genre}
            </span>
          </a>
        ))}
      </div>
    </div>
  );
}

