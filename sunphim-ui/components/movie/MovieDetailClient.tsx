// ============================================================
// SunPhim — Movie Detail Page Client Component
// ============================================================
"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import Image from "next/image";
import Link from "next/link";
import { Play, Star, Calendar, Clock, Film } from "lucide-react";
import { getMovieBySlug } from "@/lib/api";
import { buildImageUrl, formatDuration } from "@/lib/utils";
import { EpisodeList } from "@/components/movie/EpisodeList";
import { MovieRow } from "@/components/home/MovieRow";
import { ErrorBoundary } from "@/components/ui/ErrorBoundary";
import { MovieDetailSkeleton } from "@/components/ui/Skeleton";
import { useAuthStore } from "@/lib/store";
import { useToastStore } from "@/components/ui/Toast";

interface Props {
  slug: string;
}

export function MovieDetailClient({ slug }: Props) {
  const { isAuthenticated } = useAuthStore();
  const { addToast } = useToastStore();
  const [userRating, setUserRating] = useState(0);

  const { data: movie, isLoading, isError, error } = useQuery({
    queryKey: ["movie", slug],
    queryFn: () => getMovieBySlug(slug),
  });

  const handleRate = async (score: number) => {
    if (!isAuthenticated) {
      addToast("Vui lòng đăng nhập để đánh giá phim.", "error");
      return;
    }
    setUserRating(score);
    addToast(`Bạn đã đánh giá ${score}/10 cho "${movie?.name}"`, "success");
  };

  if (isLoading) return <MovieDetailSkeleton />;

  if (isError || !movie) {
    return (
      <div className="min-h-screen flex flex-col items-center justify-center pt-14">
        <div className="text-center">
          <Film className="w-16 h-16 text-[#e50914] mx-auto mb-4" />
          <h1 className="text-2xl font-bold text-white mb-2">Không tìm thấy phim</h1>
          <p className="text-[#b3b3b3] mb-6">
            {error instanceof Error ? error.message : "Phim này không tồn tại hoặc đã bị xóa."}
          </p>
          <Link href="/" className="bg-[#e50914] text-white px-6 py-2.5 rounded-md font-semibold hover:bg-[#b20710] transition-colors">
            Về trang chủ
          </Link>
        </div>
      </div>
    );
  }

  const score = movie.rating ?? movie.imdbScore;
  const episodeText =
    movie.type === "series" || movie.type === "Series"
      ? `${movie.episodeCount ?? movie.episodes?.length ?? "?"} tập`
      : "Phim lẻ";
  const backdropUrl = buildImageUrl(movie.poster || movie.thumb, false);
  const posterUrl = buildImageUrl(movie.poster || movie.thumb, false);
  const firstEpisode = movie.episodes?.[0];
  const displayScore = userRating > 0 ? userRating : (score ?? 0);

  return (
    <div className="min-h-screen pb-12">
      {/* Backdrop */}
      <div className="relative h-[50vh] min-h-[300px] max-h-[500px] overflow-hidden">
        {backdropUrl && (
          <Image
            src={backdropUrl}
            alt={movie.name}
            fill
            priority
            className="object-cover object-center opacity-30 blur-sm scale-110"
            sizes="100vw"
            unoptimized
          />
        )}
        <div className="absolute inset-0 bg-gradient-to-t from-[#141414] via-[#141414]/60 to-transparent" />
      </div>

      {/* Content */}
      <div className="max-w-[1400px] mx-auto px-[4%] -mt-48 relative z-10">
        <div className="flex flex-col lg:flex-row gap-8">
          {/* Poster */}
          <div className="flex-none mx-auto lg:mx-0">
            <div className="relative w-52 sm:w-60 aspect-[2/3] rounded-lg overflow-hidden shadow-lg">
              {posterUrl ? (
                <Image
                  src={posterUrl}
                  alt={movie.name}
                  fill
                  className="object-cover"
                  sizes="240px"
                  unoptimized
                />
              ) : (
                <div className="absolute inset-0 bg-[#1f1f1f] flex items-center justify-center">
                  <Film className="w-16 h-16 text-[#2a2a2a]" />
                </div>
              )}
            </div>
            {firstEpisode && (
              <Link
                href={`/watch/${movie.slug}?ep=${firstEpisode.episodeNumber}`}
                className="mt-4 w-full flex items-center justify-center gap-2 bg-[#e50914] hover:bg-[#b20710] text-white font-semibold px-4 py-3 rounded-md transition-colors"
              >
                <Play size={16} fill="currentColor" />
                Phát ngay
              </Link>
            )}
          </div>

          {/* Info */}
          <div className="flex-1 space-y-5">
            <div>
              <h1 className="text-2xl sm:text-3xl md:text-4xl font-bold text-white leading-tight">
                {movie.name}
              </h1>
              {movie.originName && movie.originName !== movie.name && (
                <p className="text-[#808080] text-lg mt-1">{movie.originName}</p>
              )}
            </div>

            <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-[#b3b3b3]">
              {score != null && score > 0 && (
                <span className="flex items-center gap-1 text-[#f5c518] font-bold text-base">
                  <Star size={16} className="fill-[#f5c518]" />
                  {score.toFixed(1)}
                </span>
              )}
              {movie.year && (
                <span className="flex items-center gap-1">
                  <Calendar size={14} /> {movie.year}
                </span>
              )}
              <span className="flex items-center gap-1">
                <Clock size={14} /> {formatDuration(movie.duration) || "N/A"}
              </span>
              <span className="flex items-center gap-1">
                <Film size={14} /> {episodeText}
              </span>
              {movie.quality && (
                <span className="border border-white/30 px-2 py-0.5 rounded text-xs font-medium">
                  {movie.quality}
                </span>
              )}
              {movie.lang && (
                <span className="text-xs px-2 py-0.5 bg-white/10 rounded">
                  {movie.lang}
                </span>
              )}
            </div>

            {/* User rating */}
            <div>
              <p className="text-sm text-[#808080] mb-1.5">Đánh giá của bạn:</p>
              <div className="flex items-center gap-1">
                {Array.from({ length: 10 }).map((_, i) => i + 1).map((n) => (
                  <button
                    key={n}
                    onClick={() => handleRate(n)}
                    className="w-7 h-7 rounded-sm flex items-center justify-center text-xs font-bold transition-colors"
                    style={{
                      backgroundColor:
                        n <= displayScore ? "#f5c518" : "rgba(255,255,255,0.1)",
                      color: n <= displayScore ? "#141414" : "#b3b3b3",
                    }}
                  >
                    {n}
                  </button>
                ))}
                {displayScore > 0 && (
                  <span className="ml-2 text-sm text-[#f5c518] font-semibold">
                    {displayScore}/10
                  </span>
                )}
              </div>
            </div>

            {movie.categories && movie.categories.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {movie.categories.map((cat) => (
                  <Link
                    key={cat}
                    href={`/category/${cat.toLowerCase().replace(/\s+/g, "-")}`}
                    className="text-xs px-3 py-1.5 bg-[#1f1f1f] hover:bg-[#2a2a2a] border border-white/10 hover:border-white/20 rounded-full text-[#b3b3b3] hover:text-white transition-all"
                  >
                    {cat}
                  </Link>
                ))}
              </div>
            )}

            {movie.description && (
              <div>
                <h3 className="text-sm font-semibold text-white mb-2">Nội dung</h3>
                <p className="text-sm text-[#b3b3b3] leading-relaxed max-w-3xl">
                  {movie.description}
                </p>
              </div>
            )}

            {movie.episodes && movie.episodes.length > 0 && (
              <div>
                <h3 className="text-sm font-semibold text-white mb-3 flex items-center gap-1.5">
                  <Play size={14} className="text-[#e50914]" />
                  Danh sách tập
                </h3>
                <ErrorBoundary>
                  <EpisodeList
                    episodes={movie.episodes}
                    movieSlug={movie.slug}
                  />
                </ErrorBoundary>
              </div>
            )}
          </div>
        </div>

        <div className="mt-12">
          <MovieRow
            title="Phim liên quan"
            seeAllHref={`/browse?type=${movie.type}`}
          />
        </div>
      </div>
    </div>
  );
}
