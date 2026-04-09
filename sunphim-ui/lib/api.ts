// ============================================================
// SunPhim — Typed API Client
// ============================================================
import type {
  HomePageDto,
  MovieListDto,
  MovieDetailDto,
  CategoryListDto,
  EpisodeDto,
  ApiResponse,
  SearchResult,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  WatchHistoryItem,
  CrawlResult,
} from "@/types";
import { useAuthStore } from "./store";

// API base: use environment variable, fallback to relative path (works when frontend & backend share same domain)
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "/api";

// ---------- Generic fetch wrapper ----------

async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token = useAuthStore.getState().token;

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string> || {}),
  };

  if (token) {
    (headers as Record<string, string>)["Authorization"] = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
  });

  if (!res.ok) {
    const errorText = await res.text().catch(() => "Unknown error");
    throw new Error(`API Error ${res.status}: ${errorText}`);
  }

  // Some endpoints return raw arrays/objects without wrapping
  const contentType = res.headers.get("content-type") || "";
  if (contentType.includes("application/json")) {
    return res.json() as Promise<T>;
  }
  return res.text() as unknown as T;
}

// ---------- Home ----------

export async function getHomePage(): Promise<HomePageDto> {
  return apiFetch<HomePageDto>("/home");
}

export async function getFeatured(limit = 10): Promise<MovieListDto[]> {
  return apiFetch<MovieListDto[]>(`/home/featured?limit=${limit}`);
}

export async function getTrending(limit = 20): Promise<MovieListDto[]> {
  return apiFetch<MovieListDto[]>(`/home/trending?limit=${limit}`);
}

export async function getNewReleases(limit = 20): Promise<MovieListDto[]> {
  return apiFetch<MovieListDto[]>(`/home/new-releases?limit=${limit}`);
}

export async function getTopRated(limit = 20): Promise<MovieListDto[]> {
  return apiFetch<MovieListDto[]>(`/home/top-rated?limit=${limit}`);
}

export async function getRandomFeatured(count = 5): Promise<MovieListDto[]> {
  return apiFetch<MovieListDto[]>(`/home/random-featured?count=${count}`);
}

// ---------- Movies ----------

export async function getMovies(type?: string): Promise<MovieListDto[]> {
  const qs = type ? `?type=${type}` : "";
  return apiFetch<MovieListDto[]>(`/movies${qs}`);
}

export async function getMovieBySlug(slug: string): Promise<MovieDetailDto> {
  return apiFetch<MovieDetailDto>(`/movies/slug/${slug}`);
}

export async function getMoviesByCategory(
  categorySlug: string
): Promise<MovieListDto[]> {
  return apiFetch<MovieListDto[]>(`/movies/category/${categorySlug}`);
}

export async function searchMovies(keyword: string): Promise<MovieListDto[]> {
  return apiFetch<MovieListDto[]>(`/movies/search?keyword=${encodeURIComponent(keyword)}`);
}

// ---------- Episodes ----------

export async function getEpisodes(movieId: number): Promise<EpisodeDto[]> {
  return apiFetch<EpisodeDto[]>(`/episodes/movie/${movieId}`);
}

export async function getEpisode(
  movieId: number,
  episodeNumber: number
): Promise<EpisodeDto> {
  return apiFetch<EpisodeDto>(
    `/episodes/movie/${movieId}/episode/${episodeNumber}`
  );
}

// ---------- Categories ----------

export async function getCategories(): Promise<CategoryListDto[]> {
  return apiFetch<CategoryListDto[]>("/categories");
}

export async function getCategoryBySlug(
  slug: string
): Promise<CategoryListDto> {
  return apiFetch<CategoryListDto>(`/categories/slug/${slug}`);
}

// ---------- Auth ----------

export async function login(data: LoginRequest): Promise<AuthResponse> {
  return apiFetch<AuthResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  return apiFetch<AuthResponse>("/auth/register", {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function getMe(): Promise<{ user: { id: number; email: string; username: string; avatarUrl?: string; createdAt: string } }> {
  return apiFetch("/auth/me");
}

export async function getWatchHistory(): Promise<WatchHistoryItem[]> {
  return apiFetch<WatchHistoryItem[]>("/auth/history");
}

export async function addWatchHistory(data: {
  movieId: number;
  episodeId: number;
}): Promise<{ message: string }> {
  return apiFetch<{ message: string }>("/auth/history", {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function rateMovie(
  movieId: number,
  score: number
): Promise<{ message: string }> {
  return apiFetch<{ message: string }>(`/movies/${movieId}/rate`, {
    method: "POST",
    body: JSON.stringify({ score }),
  });
}

// ---------- Crawler (Admin) ----------

export async function triggerCrawlAll(): Promise<CrawlResult> {
  return apiFetch<CrawlResult>("/crawler/run-now?maxPages=3", {
    method: "POST",
  });
}

// ---------- Image Proxy ----------

export function imageProxyUrl(
  url: string,
  width?: number
): string {
  const params = new URLSearchParams({ url });
  if (width) params.set("width", width.toString());
  return `/api/image/proxy?${params.toString()}`;
}
