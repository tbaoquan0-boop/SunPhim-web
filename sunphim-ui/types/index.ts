// ============================================================
// SunPhim — TypeScript Types
// ============================================================

// ---------- Base Movie ----------

export interface MovieListDto {
  id: number;
  name: string;
  slug: string;
  originName: string;
  thumb: string;
  poster: string;
  year: number;
  type: "series" | "single" | "movie" | string;
  quality: string;
  lang: string;
  imdbScore: number | null;
  rating: number | null;
  ratingCount: number;
  viewCount: number;
  status: string;
  categories: string[];
  episodeCount: number | null;
  updatedAt: string;
  description?: string;
}

export interface EpisodeDto {
  id: number;
  name: string;
  episodeNumber: number;
  embedLink: string | null;
  fileUrl: string | null;
  server: string;
  status: string;
}

export interface MovieDetailDto extends MovieListDto {
  description: string;
  duration: string;
  metaTitle: string;
  metaDescription: string;
  canonicalUrl: string;
  ogTitle: string;
  ogDescription: string;
  ogImage: string;
  schemaMarkup: string;
  episodes: EpisodeDto[];
}

// ---------- Category ----------

export interface CategoryListDto {
  id: number;
  name: string;
  slug: string;
  description: string;
  movieCount: number;
  isFeatured: boolean;
}

// ---------- Home Page ----------

export interface CategorySection {
  id: number;
  name: string;
  slug: string;
  movies: MovieListDto[];
}

export interface BannerDto {
  id: number;
  imageUrl: string;
  linkUrl: string;
  altText: string;
  title: string;
  subtitle: string;
}

export interface HomePageDto {
  featured: MovieListDto[];
  trending: MovieListDto[];
  newReleases: MovieListDto[];
  topRated: MovieListDto[];
  categories: CategorySection[];
  seriesList: MovieListDto[];
  singleMovies: MovieListDto[];
  heroBanner: BannerDto | null;
}

// ---------- API Response ----------

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  total: number;
  page: number;
}

// ---------- Auth ----------

export interface User {
  id: number;
  email: string;
  username: string;
  avatarUrl?: string;
  createdAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  username: string;
}

export interface AuthResponse {
  user: User;
  token: string;
  expiresAt: string;
}

export interface WatchHistoryItem {
  movieId: number;
  movieName: string;
  movieSlug: string;
  moviePoster: string;
  episodeNumber: number;
  watchedAt: string;
  progress?: number;
}

export interface RatingRequest {
  movieId: number;
  score: number;
}

// ---------- Crawler ----------

export interface CrawlResult {
  success: boolean;
  itemsProcessed: number;
  itemsAdded: number;
  itemsUpdated: number;
  errors: string[];
  duration: string;
}

// ---------- Search ----------

export interface SearchResult {
  movies: MovieListDto[];
  total: number;
  query: string;
}
