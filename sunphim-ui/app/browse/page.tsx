// ============================================================
// SunPhim — Browse Page
// ============================================================
"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Film, SlidersHorizontal } from "lucide-react";
import { getMovies, getCategories } from "@/lib/api";
import { MovieCard } from "@/components/movie/MovieCard";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/ErrorBoundary";
import Link from "next/link";
import { cn } from "@/lib/utils";

const TYPE_OPTIONS = [
  { value: "", label: "Tất cả" },
  { value: "series", label: "Phim bộ" },
  { value: "single", label: "Phim lẻ" },
];

const SORT_OPTIONS = [
  { value: "updated", label: "Mới cập nhật" },
  { value: "rating", label: "Đánh giá cao" },
  { value: "view", label: "Lượt xem" },
  { value: "year", label: "Năm phát hành" },
];

function BrowseContent() {
  const searchParams = useSearchParams();
  const type = searchParams.get("type") || "";
  const sort = searchParams.get("sort") || "updated";
  const category = searchParams.get("category") || "";

  const { data: movies, isLoading, isError } = useQuery({
    queryKey: ["movies", type],
    queryFn: () => getMovies(type || undefined),
  });

  const { data: categories } = useQuery({
    queryKey: ["categories"],
    queryFn: getCategories,
  });

  const sortedMovies = movies
    ? [...movies].sort((a, b) => {
        if (sort === "rating") return (b.rating ?? 0) - (a.rating ?? 0);
        if (sort === "view") return b.viewCount - a.viewCount;
        if (sort === "year") return b.year - a.year;
        return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime();
      })
    : [];

  const pageTitle = type === "series"
    ? "Phim bộ"
    : type === "single"
    ? "Phim lẻ"
    : "Duyệt phim";

  return (
    <div className="min-h-screen pt-14 pb-12">
      {/* Header */}
      <div className="bg-[#0a0a0a] border-b border-white/5 px-[4%] py-8">
        <div className="max-w-[1400px] mx-auto">
          <h1 className="text-3xl font-bold text-white mb-1">{pageTitle}</h1>
          <p className="text-sm text-[#808080]">
            {isLoading ? "Đang tải..." : `${sortedMovies.length} phim`}
          </p>
        </div>
      </div>

      <div className="max-w-[1400px] mx-auto px-[4%] py-6">
        {/* Filters */}
        <div className="flex flex-wrap gap-4 mb-6">
          {/* Type filter */}
          <div className="flex items-center gap-2">
            <Film size={14} className="text-[#808080]" />
            <div className="flex bg-[#1f1f1f] rounded-md overflow-hidden border border-white/10">
              {TYPE_OPTIONS.map((opt) => (
                <Link
                  key={opt.value}
                  href={`/browse${opt.value ? `?type=${opt.value}` : ""}`}
                  className={cn(
                    "px-3 py-1.5 text-xs font-medium transition-colors",
                    type === opt.value
                      ? "bg-[#e50914] text-white"
                      : "text-[#b3b3b3] hover:bg-white/10"
                  )}
                >
                  {opt.label}
                </Link>
              ))}
            </div>
          </div>

          {/* Sort */}
          <div className="flex items-center gap-2">
            <SlidersHorizontal size={14} className="text-[#808080]" />
            <select
              value={sort}
              onChange={(e) => {
                const params = new URLSearchParams(searchParams.toString());
                params.set("sort", e.target.value);
                window.location.href = `/browse?${params.toString()}`;
              }}
              className="bg-[#1f1f1f] border border-white/10 text-xs text-[#b3b3b3] rounded-md px-3 py-1.5 focus:outline-none focus:border-[#e50914]"
            >
              {SORT_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Categories chips */}
        {categories && categories.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-8">
            {category && (
              <Link
                href="/browse"
                className="px-3 py-1.5 bg-[#e50914] text-white text-xs rounded-full font-medium"
              >
                ✕ Bỏ lọc
              </Link>
            )}
            {categories.map((cat) => (
              <Link
                key={cat.id}
                href={`/browse?category=${cat.slug}${type ? `&type=${type}` : ""}`}
                className={cn(
                  "px-3 py-1.5 text-xs rounded-full font-medium border transition-colors",
                  category === cat.slug
                    ? "bg-[#e50914] border-[#e50914] text-white"
                    : "bg-[#1f1f1f] border-white/10 text-[#b3b3b3] hover:border-white/20 hover:text-white"
                )}
              >
                {cat.name}
              </Link>
            ))}
          </div>
        )}

        {/* Movie grid */}
        {isLoading ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
            {Array.from({ length: 18 }).map((_, i) => (
              <div key={i}>
                <Skeleton className="w-full aspect-[2/3] rounded-md" />
                <div className="mt-1.5 space-y-1">
                  <Skeleton className="h-3 w-3/4" />
                  <Skeleton className="h-2 w-1/2" />
                </div>
              </div>
            ))}
          </div>
        ) : isError ? (
          <EmptyState message="Đã xảy ra lỗi khi tải danh sách phim." />
        ) : sortedMovies.length === 0 ? (
          <EmptyState message="Chưa có phim nào trong danh mục này." />
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
            {sortedMovies.map((movie) => (
              <MovieCard key={movie.id} movie={movie} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default function BrowsePage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen pt-14 flex items-center justify-center">
        <div className="animate-pulse text-[#b3b3b3]">Đang tải...</div>
      </div>
    }>
      <BrowseContent />
    </Suspense>
  );
}
