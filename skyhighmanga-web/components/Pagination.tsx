'use client';

import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  baseUrl?: string;
}

function PaginationContent({ currentPage, totalPages, baseUrl = '/mangas' }: PaginationProps) {
  const searchParams = useSearchParams();
  
  // Build URL with existing query params
  const buildUrl = (page: number) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set('page', page.toString());
    return `${baseUrl}?${params.toString()}`;
  };

  // Generate page numbers to display
  const getPageNumbers = () => {
    const pages: (number | string)[] = [];
    const maxVisible = 7;
    
    if (totalPages <= maxVisible) {
      // Show all pages if total is small
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Always show first page
      pages.push(1);
      
      if (currentPage > 3) {
        pages.push('...');
      }
      
      // Show pages around current page
      const start = Math.max(2, currentPage - 1);
      const end = Math.min(totalPages - 1, currentPage + 1);
      
      for (let i = start; i <= end; i++) {
        pages.push(i);
      }
      
      if (currentPage < totalPages - 2) {
        pages.push('...');
      }
      
      // Always show last page
      pages.push(totalPages);
    }
    
    return pages;
  };

  if (totalPages <= 1) {
    return null;
  }

  return (
    <div className="flex justify-center items-center gap-2 mt-8 mb-8">
      {/* Previous Button */}
      {currentPage > 1 ? (
        <Link
          href={buildUrl(currentPage - 1)}
          className="px-4 py-2 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
        >
          ← Trước
        </Link>
      ) : (
        <span className="px-4 py-2 bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-600 border border-gray-300 dark:border-gray-600 rounded-lg cursor-not-allowed">
          ← Trước
        </span>
      )}

      {/* Page Numbers */}
      <div className="flex gap-1">
        {getPageNumbers().map((page, index) => {
          if (page === '...') {
            return (
              <span
                key={`ellipsis-${index}`}
                className="px-3 py-2 text-gray-500 dark:text-gray-400"
              >
                ...
              </span>
            );
          }

          const pageNum = page as number;
          const isActive = pageNum === currentPage;

          return (
            <Link
              key={pageNum}
              href={buildUrl(pageNum)}
              className={`px-4 py-2 rounded-lg transition-colors ${
                isActive
                  ? 'bg-purple-600 text-white dark:bg-purple-500'
                  : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'
              }`}
            >
              {pageNum}
            </Link>
          );
        })}
      </div>

      {/* Next Button */}
      {currentPage < totalPages ? (
        <Link
          href={buildUrl(currentPage + 1)}
          className="px-4 py-2 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
        >
          Sau →
        </Link>
      ) : (
        <span className="px-4 py-2 bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-600 border border-gray-300 dark:border-gray-600 rounded-lg cursor-not-allowed">
          Sau →
        </span>
      )}
    </div>
  );
}

export default function Pagination(props: PaginationProps) {
  return (
    <Suspense fallback={<div className="h-12"></div>}>
      <PaginationContent {...props} />
    </Suspense>
  );
}

