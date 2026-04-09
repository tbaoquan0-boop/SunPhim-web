// ============================================================
// SunPhim — Utility Functions
// ============================================================

export function cn(...classes: (string | undefined | null | false)[]): string {
  return classes.filter(Boolean).join(" ");
}

export function movieThumbById(movie: { thumb?: string; id: number }, width = 300): string {
  if (movie.thumb) return movie.thumb;
  return `https://api.bopimo.com/thumb/${movie.id}/${width}`;
}

export function moviePosterById(
  movie: { poster?: string; id: number },
  width = 780
): string {
  if (movie.poster) return movie.poster;
  return `https://api.bopimo.com/poster/${movie.id}/${width}`;
}

export function buildImageUrl(url: string | null | undefined, fallback = true): string {
  if (!url || url.trim() === "") {
    return fallback ? "/img/placeholder.svg" : "";
  }
  return url;
}

export function formatYear(dateStr?: string | null): string {
  if (!dateStr) return "";
  const d = new Date(dateStr);
  return isNaN(d.getTime()) ? "" : d.getFullYear().toString();
}

export function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(d);
}

export function formatDuration(minutes?: string | null): string {
  if (!minutes) return "";
  const m = parseInt(minutes, 10);
  if (isNaN(m)) return "";
  const h = Math.floor(m / 60);
  const r = m % 60;
  if (h === 0) return `${m} phút`;
  return r > 0 ? `${h}h ${r}p` : `${h}h`;
}

export function truncate(str: string, maxLen: number): string {
  if (str.length <= maxLen) return str;
  return str.slice(0, maxLen - 3) + "...";
}

export function isNewMovie(year?: number): boolean {
  const currentYear = new Date().getFullYear();
  return year !== undefined && year >= currentYear - 1;
}

export function slugify(text: string): string {
  return text
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

export function debounce<T extends (...args: any[]) => any>(
  fn: T,
  delay: number
): (...args: Parameters<T>) => ReturnType<T> | void {
  let timer: ReturnType<typeof setTimeout>;
  return (...args: Parameters<T>) => {
    clearTimeout(timer);
    timer = setTimeout(() => fn(...args), delay);
  };
}
