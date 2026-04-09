// ============================================================
// SunPhim — Home Page
// ============================================================
"use client";

import { useQuery } from "@tanstack/react-query";
import { getHomePage, getCategories } from "@/lib/api";
import { HeroCarousel } from "@/components/home/HeroCarousel";
import { MovieRow } from "@/components/home/MovieRow";
import { ErrorBoundary, EmptyState } from "@/components/ui/ErrorBoundary";
import Link from "next/link";
import { ChevronRight } from "lucide-react";

export default function HomePage() {
  const { data: homeData, isLoading: homeLoading, isError: homeError } = useQuery({
    queryKey: ["home"],
    queryFn: getHomePage,
  });

  const { data: categories } = useQuery({
    queryKey: ["categories"],
    queryFn: getCategories,
  });

  return (
    <div className="min-h-screen">
      {/* Hero */}
      <ErrorBoundary>
        <HeroCarousel
          movies={homeData?.featured}
          isLoading={homeLoading}
        />
      </ErrorBoundary>

      {/* Main content - overlaps hero bottom */}
      <div className="relative z-10 -mt-8 md:-mt-16">
        {/* Trending */}
        <ErrorBoundary>
          <MovieRow
            title="Xu hướng"
            movies={homeData?.trending}
            isLoading={homeLoading}
            seeAllHref="/browse"
          />
        </ErrorBoundary>

        {/* New Releases */}
        <ErrorBoundary>
          <MovieRow
            title="Phim mới cập nhật"
            movies={homeData?.newReleases}
            isLoading={homeLoading}
            seeAllHref="/browse?sort=new"
          />
        </ErrorBoundary>

        {/* Top Rated */}
        <ErrorBoundary>
          <MovieRow
            title="Top đánh giá"
            movies={homeData?.topRated}
            isLoading={homeLoading}
            seeAllHref="/browse?sort=rating"
          />
        </ErrorBoundary>

        {/* Series */}
        <ErrorBoundary>
          <MovieRow
            title="Phim bộ"
            movies={homeData?.seriesList}
            isLoading={homeLoading}
            seeAllHref="/browse?type=series"
          />
        </ErrorBoundary>

        {/* Single Movies */}
        <ErrorBoundary>
          <MovieRow
            title="Phim lẻ"
            movies={homeData?.singleMovies}
            isLoading={homeLoading}
            seeAllHref="/browse?type=single"
          />
        </ErrorBoundary>

        {/* Categories */}
        {categories && categories.length > 0 && (
          <section className="py-6 px-[4%]">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-white">Danh mục thể loại</h2>
            </div>
            <div className="flex flex-wrap gap-2">
              {categories.map((cat) => (
                <Link
                  key={cat.id}
                  href={`/category/${cat.slug}`}
                  className="group flex items-center gap-2 px-4 py-2 bg-[#1f1f1f] hover:bg-[#2a2a2a] border border-white/10 hover:border-[#e50914]/50 rounded-full text-sm text-[#b3b3b3] hover:text-white transition-all duration-200"
                >
                  {cat.name}
                  <span className="text-[10px] text-[#808080] group-hover:text-[#e50914] transition-colors">
                    {cat.movieCount}
                  </span>
                  <ChevronRight size={12} className="opacity-0 group-hover:opacity-100 transition-opacity" />
                </Link>
              ))}
            </div>
          </section>
        )}

        {/* Category Sections (if returned from API) */}
        {homeData?.categories && homeData.categories.length > 0 && (
          homeData.categories.map((section) => (
            <ErrorBoundary key={section.id}>
              <MovieRow
                title={section.name}
                movies={section.movies}
                seeAllHref={`/category/${section.slug}`}
              />
            </ErrorBoundary>
          ))
        )}
      </div>

      {/* Bottom spacing */}
      <div className="pb-12" />
    </div>
  );
}
