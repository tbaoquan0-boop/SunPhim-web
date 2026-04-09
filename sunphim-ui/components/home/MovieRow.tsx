// ============================================================
// SunPhim — MovieRow Component (Horizontal scroll carousel)
// ============================================================
"use client";

import { useRef, useState, useEffect, useCallback } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { MovieCard } from "@/components/movie/MovieCard";
import { EmptyState } from "@/components/ui/ErrorBoundary";
import { Skeleton } from "@/components/ui/Skeleton";
import { cn } from "@/lib/utils";
import type { MovieListDto } from "@/types";

interface MovieRowProps {
  title?: string;
  movies?: MovieListDto[];
  isLoading?: boolean;
  href?: string;
  seeAllLabel?: string;
  seeAllHref?: string;
  className?: string;
}

export function MovieRow({
  title,
  movies,
  isLoading,
  seeAllHref,
  className,
}: MovieRowProps) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);
  const [isHovered, setIsHovered] = useState(false);

  const updateScrollButtons = useCallback(() => {
    const el = scrollRef.current;
    if (!el) return;
    setCanScrollLeft(el.scrollLeft > 10);
    setCanScrollRight(el.scrollLeft < el.scrollWidth - el.clientWidth - 10);
  }, []);

  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    updateScrollButtons();
    el.addEventListener("scroll", updateScrollButtons, { passive: true });
    window.addEventListener("resize", updateScrollButtons);
    return () => {
      el.removeEventListener("scroll", updateScrollButtons);
      window.removeEventListener("resize", updateScrollButtons);
    };
  }, [updateScrollButtons]);

  const scroll = (direction: "left" | "right") => {
    const el = scrollRef.current;
    if (!el) return;
    const cardWidth = el.children[0]?.clientWidth ?? 160;
    el.scrollBy({
      left: direction === "right" ? cardWidth * 3 : -cardWidth * 3,
      behavior: "smooth",
    });
  };

  if (isLoading) {
    return (
      <div className={cn("py-4", className)}>
        {title && (
          <div className="px-[4%] mb-3">
            <Skeleton className="h-6 w-40" />
          </div>
        )}
        <div className="flex gap-3 overflow-hidden px-[4%]">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="flex-none">
              <Skeleton className="w-[140px] sm:w-[160px] aspect-[2/3] rounded-md" />
              <div className="mt-1.5 space-y-1">
                <Skeleton className="h-3 w-32" />
                <Skeleton className="h-2 w-20" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (!movies || movies.length === 0) {
    return (
      <div className={cn("py-4", className)}>
        {title && <div className="px-[4%] mb-3"><Skeleton className="h-6 w-40" /></div>}
        <EmptyState message="Chưa có phim nào" className="px-[4%]" />
      </div>
    );
  }

  return (
    <div className={cn("py-4", className)}>
      {/* Header */}
      {title && (
        <div className="flex items-center justify-between px-[4%] mb-3">
          <h2 className="text-lg font-semibold text-white">{title}</h2>
          {seeAllHref && (
            <a
              href={seeAllHref}
              className="text-sm text-[#b3b3b3] hover:text-white transition-colors"
            >
              Xem tất cả &rarr;
            </a>
          )}
        </div>
      )}

      {/* Carousel */}
      <div
        className="relative group/row"
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        {/* Prev button */}
        <button
          onClick={() => scroll("left")}
          className={cn(
            "absolute left-[2%] top-1/2 -translate-y-1/2 z-20 w-9 h-9 rounded-full bg-black/70 border border-white/20",
            "flex items-center justify-center text-white",
            "opacity-0 group-hover/row:opacity-100 transition-opacity duration-200",
            "hover:bg-black/90 disabled:opacity-30",
            canScrollLeft ? "cursor-pointer" : "cursor-not-allowed"
          )}
          disabled={!canScrollLeft}
          aria-label="Cuộn sang trái"
        >
          <ChevronLeft size={20} />
        </button>

        {/* Scrollable list */}
        <div
          ref={scrollRef}
          className="flex gap-3 overflow-x-auto hide-scrollbar scroll-snap-x px-[4%] py-1"
        >
          {movies.map((movie) => (
            <MovieCard key={movie.id} movie={movie} />
          ))}
        </div>

        {/* Next button */}
        <button
          onClick={() => scroll("right")}
          className={cn(
            "absolute right-[2%] top-1/2 -translate-y-1/2 z-20 w-9 h-9 rounded-full bg-black/70 border border-white/20",
            "flex items-center justify-center text-white",
            "opacity-0 group-hover/row:opacity-100 transition-opacity duration-200",
            "hover:bg-black/90 disabled:opacity-30",
            canScrollRight ? "cursor-pointer" : "cursor-not-allowed"
          )}
          disabled={!canScrollRight}
          aria-label="Cuộn sang phải"
        >
          <ChevronRight size={20} />
        </button>
      </div>
    </div>
  );
}
