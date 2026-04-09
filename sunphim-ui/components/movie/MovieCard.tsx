// ============================================================
// SunPhim — MovieCard Component
// ============================================================
"use client";

import Link from "next/link";
import Image from "next/image";
import { Star } from "lucide-react";
import { cn, buildImageUrl, isNewMovie } from "@/lib/utils";
import type { MovieListDto } from "@/types";

interface MovieCardProps {
  movie: MovieListDto;
  className?: string;
  size?: "sm" | "md" | "lg";
}

export function MovieCard({ movie, className, size = "md" }: MovieCardProps) {
  const score = movie.rating ?? movie.imdbScore;
  const isNew = isNewMovie(movie.year);
  const episodeText =
    movie.type === "series" || movie.type === "Series"
      ? `${movie.episodeCount ?? "?"} tập`
      : null;

  const posterUrl = buildImageUrl(movie.poster || movie.thumb, false);
  const aspectClass =
    size === "sm"
      ? "aspect-[2/3] w-[105px] sm:w-[130px]"
      : size === "lg"
      ? "aspect-[2/3] w-[160px] sm:w-[180px] md:w-[200px]"
      : "aspect-[2/3] w-[140px] sm:w-[160px]";

  return (
    <Link
      href={`/movie/${movie.slug}`}
      className={cn(
        "group flex-none relative rounded-md overflow-hidden cursor-pointer transition-all duration-200",
        "hover:scale-105 hover:z-10",
        className
      )}
    >
      {/* Poster */}
      <div className={cn("relative", aspectClass)}>
        {posterUrl ? (
          <Image
            src={posterUrl}
            alt={movie.name}
            fill
            sizes={size === "sm" ? "130px" : size === "lg" ? "200px" : "160px"}
            className="object-cover transition-transform duration-300 group-hover:scale-105"
            unoptimized
          />
        ) : (
          <div className="absolute inset-0 bg-[#1f1f1f] flex items-center justify-center">
            <span className="text-4xl opacity-30">🎬</span>
          </div>
        )}

        {/* Overlay on hover */}
        <div className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-200 flex flex-col justify-end p-2">
          <p className="text-xs font-medium text-white line-clamp-2 leading-tight">
            {movie.name}
          </p>
          <div className="flex flex-wrap gap-1 mt-1">
            {movie.quality && (
              <span className="text-[10px] px-1 py-0.5 bg-white/20 rounded text-white">
                {movie.quality}
              </span>
            )}
            {movie.lang && (
              <span className="text-[10px] px-1 py-0.5 bg-white/20 rounded text-white">
                {movie.lang}
              </span>
            )}
            {episodeText && (
              <span className="text-[10px] px-1 py-0.5 bg-white/20 rounded text-white">
                {episodeText}
              </span>
            )}
          </div>
        </div>

        {/* Badges: quality + new */}
        <div className="absolute top-1.5 left-1.5 flex flex-col gap-1">
          {movie.quality && (
            <span className="text-[10px] font-bold px-1.5 py-0.5 bg-[#e50914] text-white rounded-sm leading-none">
              {movie.quality}
            </span>
          )}
          {isNew && (
            <span className="text-[10px] font-bold px-1.5 py-0.5 bg-green-600 text-white rounded-sm leading-none">
              Mới
            </span>
          )}
        </div>

        {/* Rating */}
        {score != null && score > 0 && (
          <div className="absolute top-1.5 right-1.5 flex items-center gap-0.5 px-1.5 py-0.5 bg-black/70 rounded-sm">
            <Star size={9} className="text-[#f5c518] fill-[#f5c518]" />
            <span className="text-[10px] font-bold text-[#f5c518] leading-none">
              {score.toFixed(1)}
            </span>
          </div>
        )}
      </div>

      {/* Title below card (desktop) */}
      <div className="hidden md:block mt-1.5">
        <p className="text-xs font-medium text-white line-clamp-2 leading-tight group-hover:text-[#e50914] transition-colors">
          {movie.name}
        </p>
        <p className="text-[10px] text-[#808080] mt-0.5">
          {movie.year} {episodeText && `• ${episodeText}`}
        </p>
      </div>
    </Link>
  );
}
