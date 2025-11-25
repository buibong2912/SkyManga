// API Base URL - Sử dụng HTTP trong development để tránh SSL certificate issues
// Nếu API chạy HTTPS, đổi thành https://localhost:7153/api
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5178/api';

// Helper function để fetch với error handling tốt hơn
async function apiFetch(url: string, options?: RequestInit): Promise<Response> {
  try {
    // Trong development server-side, nếu URL là HTTPS, cần xử lý SSL
    if (process.env.NODE_ENV === 'development' && typeof window === 'undefined') {
      const urlObj = new URL(url);
      if (urlObj.protocol === 'https:') {
        // Sử dụng node-fetch với custom agent để bỏ qua SSL verification (CHỈ CHO DEV)
        const https = require('https');
        const { Agent } = https;
        const agent = new Agent({ rejectUnauthorized: false });
        
        const nodeFetch = (await import('node-fetch')).default;
        return nodeFetch(url, {
          ...options,
          agent,
        } as any);
      }
    }
    
    // Default fetch cho HTTP hoặc client-side
    return fetch(url, options);
  } catch (error) {
    console.error('API fetch error:', error);
    throw error;
  }
}

export async function fetchMangas(
  page = 1, 
  pageSize = 20,
  options?: {
    sortBy?: string;
    sortDescending?: boolean;
    status?: string;
    genreId?: string;
    authorId?: string;
  }
): Promise<{ data: any[]; total: number }> {
  try {
    const params = new URLSearchParams({
      pageNumber: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (options?.sortBy) {
      params.append('sortBy', options.sortBy);
    }
    if (options?.sortDescending !== undefined) {
      params.append('sortDescending', options.sortDescending.toString());
    }
    if (options?.status) {
      params.append('status', options.status);
    }
    if (options?.genreId) {
      params.append('genreId', options.genreId);
    }
    if (options?.authorId) {
      params.append('authorId', options.authorId);
    }

    const response = await apiFetch(`${API_BASE_URL}/manga?${params.toString()}`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      throw new Error(`Failed to fetch mangas: ${response.status} ${response.statusText}`);
    }
    
    const pagedResponse = await response.json();
    
    // API trả về PagedResponse với Items và TotalCount
    return {
      data: pagedResponse.items || pagedResponse.Items || [],
      total: pagedResponse.totalCount || pagedResponse.TotalCount || 0,
    };
  } catch (error) {
    console.error('Error fetching mangas:', error);
    // Return empty data for development
    return {
      data: [],
      total: 0,
    };
  }
}

export async function fetchMangaById(id: string): Promise<any> {
  try {
    const response = await apiFetch(`${API_BASE_URL}/manga/${id}`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      throw new Error(`Failed to fetch manga: ${response.status} ${response.statusText}`);
    }
    
    return await response.json();
  } catch (error) {
    console.error('Error fetching manga:', error);
    return null;
  }
}

export async function fetchMangaChapters(mangaId: string): Promise<any[]> {
  try {
    // Validate GUID format
    if (!mangaId || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(mangaId)) {
      console.error('Invalid mangaId format:', mangaId);
      return [];
    }

    // API endpoint: GET /api/chapter/manga/{mangaId}
    const url = `${API_BASE_URL}/chapter/manga/${mangaId}`;
    console.log('Fetching chapters from:', url);
    
    const response = await apiFetch(url, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      const errorText = await response.text().catch(() => 'Unknown error');
      console.error(`Failed to fetch chapters: ${response.status} ${response.statusText}`, errorText);
      throw new Error(`Failed to fetch chapters: ${response.status} ${response.statusText}`);
    }
    
    const data = await response.json();
    // API trả về List<ChapterDto> trực tiếp
    return Array.isArray(data) ? data : [];
  } catch (error) {
    console.error('Error fetching chapters:', error);
    return [];
  }
}

export async function fetchChapterPages(chapterId: string): Promise<any[]> {
  try {
    // API endpoint: GET /api/page/chapter/{chapterId}
    const response = await apiFetch(`${API_BASE_URL}/page/chapter/${chapterId}`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      throw new Error(`Failed to fetch pages: ${response.status} ${response.statusText}`);
    }
    
    const data = await response.json();
    // API trả về List<PageDto> trực tiếp
    return Array.isArray(data) ? data : [];
  } catch (error) {
    console.error('Error fetching pages:', error);
    return [];
  }
}

export async function searchMangas(query: string): Promise<any[]> {
  try {
    // API sử dụng searchTerm query parameter và trả về PagedResponse
    const response = await apiFetch(`${API_BASE_URL}/manga?searchTerm=${encodeURIComponent(query)}&pageNumber=1&pageSize=50`, {
      cache: 'no-store',
    });
    
    if (!response.ok) {
      throw new Error(`Failed to search mangas: ${response.status} ${response.statusText}`);
    }
    
    const pagedResponse = await response.json();
    // API trả về PagedResponse với Items
    return pagedResponse.items || pagedResponse.Items || [];
  } catch (error) {
    console.error('Error searching mangas:', error);
    return [];
  }
}
