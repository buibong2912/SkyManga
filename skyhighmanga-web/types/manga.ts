export interface Manga {
  id: string;
  title: string;
  description?: string;
  coverImageUrl?: string;
  author?: {
    id: string;
    name: string;
  };
  genres?: Genre[];
  status: MangaStatus;
  rating?: number;
  viewCount?: number;
  sourceUrl?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface Genre {
  id: string;
  name: string;
}

export enum MangaStatus {
  Unknown = "Unknown",
  Ongoing = "Ongoing",
  Completed = "Completed",
  OnHold = "OnHold",
  Cancelled = "Cancelled",
}

export interface Chapter {
  id: string;
  title: string;
  chapterNumber?: string;
  chapterIndex?: number;
  mangaId: string;
  sourceUrl?: string;
  publishedAt?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface Page {
  id: string;
  pageNumber: number;
  imageUrl: string;
  chapterId: string;
  isDownloaded: boolean;
}

export interface MangaDetail extends Manga {
  chapters: Chapter[];
}

export interface ChapterDetail extends Chapter {
  manga: Manga;
  pages: Page[];
}

