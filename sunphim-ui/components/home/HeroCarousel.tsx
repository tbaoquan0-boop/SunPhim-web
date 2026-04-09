// ============================================================
// SunPhim — Hero Carousel Component
// ============================================================
"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import Link from "next/link";
import Image from "next/image";
import { Play, Info, Star } from "lucide-react";
import { cn, buildImageUrl } from "@/lib/utils";
import { Skeleton } from "@/components/ui/Skeleton";
import type { MovieListDto } from "@/types";

interface HeroCarouselProps {
  movies?: MovieListDto[];
  isLoading?: boolean;
}

export function HeroCarousel({ movies, isLoading }: HeroCarouselProps) {
  const [current, setCurrent] = useState(0);
  const [isPaused, setIsPaused] = useState(false);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const slides = movies && movies.length > 0 ? movies.slice(0, 5) : [];
  const total = slides.length;

  const goTo = useCallback(
    (index: number) => {
      setCurrent(((index % total) + total) % total);
    },
    [total]
  );

  const next = useCallback(() => goTo(current + 1), [current, goTo]);

  useEffect(() => {
    if (total === 0 || isPaused) return;
    timerRef.current = setInterval(next, 6000);
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [total, isPaused, next]);

  // Touch/swipe support
  const touchStartX = useRef<number | null>(null);
  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartX.current = e.touches[0].clientX;
  };
  const handleTouchEnd = (e: React.TouchEvent) => {
    if (touchStartX.current === null) return;
    const diff = touchStartX.current - e.changedTouches[0].clientX;
    if (Math.abs(diff) > 50) {
      goTo(current + (diff > 0 ? 1 : -1));
    }
    touchStartX.current = null;
  };

  if (isLoading || slides.length === 0) {
    return <HeroSkeleton />;
  }

  const movie = slides[current];
  const score = movie.rating ?? movie.imdbScore;
  const episodeText =
    movie.type === "series" || movie.type === "Series"
      ? `${movie.episodeCount ?? "?"} tập`
      : null;
  const bgUrl = buildImageUrl(movie.poster, false);

  return (
    <section
      className="relative w-full h-[70vh] min-h-[420px] max-h-[750px] overflow-hidden bg-black select-none"
      onMouseEnter={() => setIsPaused(true)}
      onMouseLeave={() => setIsPaused(false)}
      onTouchStart={handleTouchStart}
      onTouchEnd={handleTouchEnd}
    >
      {/* Background image */}
      <div className="absolute inset-0">
        {bgUrl && (
          <Image
            src={bgUrl}
            alt={movie.name}
            fill
            priority
            className="object-cover object-center opacity-40 scale-105"
            sizes="100vw"
            unoptimized
          />
        )}
        <div className="absolute inset-0 bg-gradient-to-r from-[#141414] via-[#141414]/60 to-transparent" />
        <div className="absolute inset-0 bg-gradient-to-t from-[#141414] via-transparent to-transparent" />
      </div>

      {/* Content */}
      <div className="relative z-10 h-full flex items-center px-[4%] md:px-16 max-w-[1400px] mx-auto">
        <div className="max-w-xl space-y-4">
          <h1 className="text-3xl sm:text-4xl md:text-5xl lg:text-6xl font-bold text-white leading-tight line-clamp-2">
            {movie.name}
          </h1>

          {/* Meta */}
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-[#b3b3b3]">
            {score != null && score > 0 && (
              <span className="flex items-center gap-1 text-[#f5c518] font-semibold">
                <Star size={14} className="fill-[#f5c518]" />
                {score.toFixed(1)}
              </span>
            )}
            {movie.year && <span>{movie.year}</span>}
            {movie.quality && (
              <span className="border border-white/30 px-1.5 py-0.5 rounded text-xs">
                {movie.quality}
              </span>
            )}
            {movie.lang && <span>{movie.lang}</span>}
            {episodeText && <span>{episodeText}</span>}
          </div>

          {/* Description */}
          {movie.description && (
            <p className="text-sm text-[#b3b3b3] line-clamp-2 md:line-clamp-3 max-w-lg">
              {movie.description}
            </p>
          )}

          {/* Categories */}
          {movie.categories && movie.categories.length > 0 && (
            <div className="flex flex-wrap gap-1.5">
              {movie.categories.slice(0, 4).map((cat) => (
                <span
                  key={cat}
                  className="text-xs px-2 py-0.5 bg-white/10 rounded-full text-[#b3b3b3]"
                >
                  {cat}
                </span>
              ))}
            </div>
          )}

          {/* Actions */}
          <div className="flex flex-wrap gap-3 pt-2">
            <Link
              href={`/watch/${movie.slug}?ep=1`}
              className="flex items-center gap-2 bg-[#e50914] hover:bg-[#b20710] text-white font-semibold px-6 py-2.5 rounded-md transition-colors text-sm"
            >
              <Play size={16} fill="currentColor" />
              Phát ngay
            </Link>
            <Link
              href={`/movie/${movie.slug}`}
              className="flex items-center gap-2 bg-white/20 hover:bg-white/30 text-white font-semibold px-6 py-2.5 rounded-md transition-colors text-sm backdrop-blur-sm"
            >
              <Info size={16} />
              Chi tiết
            </Link>
          </div>
        </div>
      </div>

      {/* Dot indicators */}
      {total > 1 && (
        <div className="absolute bottom-6 left-1/2 -translate-x-1/2 z-20 flex items-center gap-2">
          {slides.map((_, i) => (
            <button
              key={i}
              onClick={() => goTo(i)}
              className={cn(
                "rounded-full transition-all duration-300",
                i === current
                  ? "w-7 h-2 bg-[#e50914]"
                  : "w-2 h-2 bg-white/40 hover:bg-white/60"
              )}
              aria-label={`Chuyển đến slide ${i + 1}`}
            />
          ))}
        </div>
      )}

      {/* Slide progress bar */}
      {total > 1 && isPaused === false && (
        <div className="absolute bottom-0 left-0 right-0 h-0.5 bg-white/10">
          <div
            key={`progress-${current}`}
            className="h-full bg-[#e50914] animate-[shrink_6s_linear]"
            style={{ animationFillMode: "forwards" }}
          />
        </div>
      )}
    </section>
  );
}

function HeroSkeleton() {
  return (
    <div className="relative w-full h-[70vh] min-h-[420px] max-h-[750px] overflow-hidden bg-black">
      <Skeleton className="w-full h-full" />
      <div className="absolute inset-0 bg-gradient-to-r from-[#141414] via-[#141414]/60 to-transparent" />
      <div className="relative z-10 h-full flex items-center px-[4%] md:px-16">
        <div className="max-w-xl space-y-4">
          <Skeleton className="h-12 w-3/4" />
          <div className="flex gap-2">
            <Skeleton className="h-4 w-16" />
            <Skeleton className="h-4 w-20" />
            <Skeleton className="h-4 w-12" />
          </div>
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
          <div className="flex gap-3 pt-2">
            <Skeleton className="h-10 w-32" />
            <Skeleton className="h-10 w-32" />
          </div>
        </div>
      </div>
    </div>
  );
}
