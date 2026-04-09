// ============================================================
// SunPhim — Search Page
// ============================================================
"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Search } from "lucide-react";
import { searchMovies } from "@/lib/api";
import { MovieCard } from "@/components/movie/MovieCard";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/ErrorBoundary";
import { debounce } from "@/lib/utils";

function SearchContent() {
  const searchParams = useSearchParams();
  const initialQuery = searchParams.get("q") || "";
  const [query, setQuery] = useState(initialQuery);
  const [debouncedQuery, setDebouncedQuery] = useState(initialQuery);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const debouncedSet = debounce((val: string) => setDebouncedQuery(val), 400);

  useEffect(() => {
    debouncedSet(query);
  }, [query, debouncedSet]);

  useEffect(() => {
    setQuery(initialQuery);
    setDebouncedQuery(initialQuery);
  }, [initialQuery]);

  const { data: movies, isLoading, isError } = useQuery({
    queryKey: ["search", debouncedQuery],
    queryFn: () => searchMovies(debouncedQuery),
    enabled: debouncedQuery.trim().length > 0,
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) {
      const url = new URL(window.location.href);
      url.searchParams.set("q", query.trim());
      window.history.pushState({}, "", url.toString());
      setDebouncedQuery(query.trim());
    }
  };

  return (
    <div className="min-h-screen pt-14 pb-12">
      {/* Search bar */}
      <div className="bg-[#0a0a0a] border-b border-white/5 px-[4%] py-8">
        <div className="max-w-[1400px] mx-auto">
          <h1 className="text-3xl font-bold text-white mb-4">Tìm kiếm</h1>
          <form onSubmit={handleSubmit} className="relative max-w-2xl">
            <Search
              size={18}
              className="absolute left-4 top-1/2 -translate-y-1/2 text-[#808080]"
            />
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Tìm tên phim, diễn viên, đạo diễn..."
              className="w-full h-12 pl-12 pr-4 bg-[#1f1f1f] border border-white/10 rounded-lg text-white placeholder-[#808080] focus:outline-none focus:border-[#e50914] transition-colors text-sm"
              autoFocus
            />
          </form>
        </div>
      </div>

      <div className="max-w-[1400px] mx-auto px-[4%] py-6">
        {!debouncedQuery && (
          <EmptyState
            message="Nhập từ khóa để tìm kiếm phim."
            icon={
              <Search size={40} className="text-[#2a2a2a]" />
            }
          />
        )}

        {debouncedQuery && isLoading && (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
            {Array.from({ length: 12 }).map((_, i) => (
              <div key={i}>
                <Skeleton className="w-full aspect-[2/3] rounded-md" />
                <div className="mt-1.5 space-y-1">
                  <Skeleton className="h-3 w-3/4" />
                  <Skeleton className="h-2 w-1/2" />
                </div>
              </div>
            ))}
          </div>
        )}

        {debouncedQuery && !isLoading && isError && (
          <EmptyState message="Đã xảy ra lỗi khi tìm kiếm." />
        )}

        {debouncedQuery && !isLoading && movies && (
          <>
            <p className="text-sm text-[#808080] mb-4">
              {movies.length === 0
                ? `Không có kết quả cho "${debouncedQuery}"`
                : `Tìm thấy ${movies.length} phim cho "${debouncedQuery}"`}
            </p>
            {movies.length === 0 ? (
              <EmptyState message={`Không tìm thấy phim nào cho "${debouncedQuery}".`} />
            ) : (
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
                {movies.map((movie) => (
                  <MovieCard key={movie.id} movie={movie} />
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}

export default function SearchPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen pt-14 flex items-center justify-center">
        <div className="animate-pulse text-[#b3b3b3]">Đang tải...</div>
      </div>
    }>
      <SearchContent />
    </Suspense>
  );
}
