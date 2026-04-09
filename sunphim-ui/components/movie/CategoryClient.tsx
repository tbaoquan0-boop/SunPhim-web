// ============================================================
// SunPhim — Category Page Client Component
// ============================================================
"use client";

import { useQuery } from "@tanstack/react-query";
import { getMoviesByCategory, getCategoryBySlug } from "@/lib/api";
import { MovieCard } from "@/components/movie/MovieCard";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/ErrorBoundary";

interface Props {
  slug: string;
}

export function CategoryContent({ slug }: Props) {
  const { data: category, isLoading: catLoading } = useQuery({
    queryKey: ["category", slug],
    queryFn: () => getCategoryBySlug(slug),
  });

  const { data: movies, isLoading, isError } = useQuery({
    queryKey: ["movies-by-category", slug],
    queryFn: () => getMoviesByCategory(slug),
  });

  return (
    <div className="min-h-screen pt-14 pb-12">
      {/* Header */}
      <div className="bg-[#0a0a0a] border-b border-white/5 px-[4%] py-8">
        <div className="max-w-[1400px] mx-auto">
          <div className="text-sm text-[#808080] mb-1">Thể loại</div>
          <h1 className="text-3xl font-bold text-white mb-1">
            {catLoading ? (
              <Skeleton className="h-8 w-48 inline-block" />
            ) : (
              category?.name || slug
            )}
          </h1>
          <p className="text-sm text-[#808080]">
            {isLoading ? "Đang tải..." : `${movies?.length ?? 0} phim`}
            {category?.description && ` — ${category.description}`}
          </p>
        </div>
      </div>

      <div className="max-w-[1400px] mx-auto px-[4%] py-6">
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
        ) : !movies || movies.length === 0 ? (
          <EmptyState message="Chưa có phim nào trong thể loại này." />
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
            {movies.map((movie) => (
              <MovieCard key={movie.id} movie={movie} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
