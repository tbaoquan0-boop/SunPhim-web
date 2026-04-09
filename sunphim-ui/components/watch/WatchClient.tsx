// ============================================================
// SunPhim — Watch Page Client Component
// ============================================================
"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import Image from "next/image";
import Link from "next/link";
import {
  ChevronLeft,
  ChevronRight,
  List,
  Info,
  Share2,
  AlertCircle,
} from "lucide-react";
import { getMovieBySlug, addWatchHistory } from "@/lib/api";
import { buildImageUrl, cn } from "@/lib/utils";
import { EpisodeList } from "@/components/movie/EpisodeList";
import { useAuthStore } from "@/lib/store";

interface Props {
  slug: string;
}

export function WatchClient({ slug }: Props) {
  const { isAuthenticated } = useAuthStore();
  const [episodeNumber, setEpisodeNumber] = useState(1);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const { data: movie, isLoading } = useQuery({
    queryKey: ["movie", slug],
    queryFn: () => getMovieBySlug(slug),
  });

  const episodes = movie?.episodes ?? [];
  const sortedEpisodes = [...episodes].sort(
    (a, b) => a.episodeNumber - b.episodeNumber
  );
  const currentEpisode = sortedEpisodes.find(
    (e) => e.episodeNumber === episodeNumber
  );

  useEffect(() => {
    if (typeof window !== "undefined") {
      const params = new URLSearchParams(window.location.search);
      const ep = parseInt(params.get("ep") ?? "1", 10);
      if (!isNaN(ep)) setEpisodeNumber(ep);
    }
  }, [slug]);

  useEffect(() => {
    if (isAuthenticated && movie && currentEpisode) {
      addWatchHistory({
        movieId: movie.id,
        episodeId: currentEpisode.id,
      }).catch(() => {});
    }
  }, [isAuthenticated, movie, currentEpisode]);

  const goToEpisode = (ep: number) => {
    setEpisodeNumber(ep);
    const url = new URL(window.location.href);
    url.searchParams.set("ep", ep.toString());
    window.history.pushState({}, "", url.toString());
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const prevEp = () => {
    const idx = sortedEpisodes.findIndex((e) => e.episodeNumber === episodeNumber);
    if (idx > 0) goToEpisode(sortedEpisodes[idx - 1].episodeNumber);
  };

  const nextEp = () => {
    const idx = sortedEpisodes.findIndex((e) => e.episodeNumber === episodeNumber);
    if (idx < sortedEpisodes.length - 1)
      goToEpisode(sortedEpisodes[idx + 1].episodeNumber);
  };

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "ArrowLeft") prevEp();
      if (e.key === "ArrowRight") nextEp();
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [episodeNumber, sortedEpisodes]);

  const handleShare = async () => {
    const url = window.location.href;
    if (navigator.share) {
      await navigator.share({ title: movie?.name, url });
    } else {
      await navigator.clipboard.writeText(url);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center pt-14">
        <div className="animate-pulse text-[#b3b3b3]">Đang tải...</div>
      </div>
    );
  }

  if (!movie) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center pt-14">
        <div className="text-center">
          <AlertCircle className="w-12 h-12 text-[#e50914] mx-auto mb-4" />
          <h2 className="text-xl font-bold text-white mb-2">Không tìm thấy phim</h2>
          <Link href="/" className="text-[#e50914] hover:underline">
            Về trang chủ
          </Link>
        </div>
      </div>
    );
  }

  const posterUrl = buildImageUrl(movie.poster || movie.thumb, false);
  const currentIdx = sortedEpisodes.findIndex(
    (e) => e.episodeNumber === episodeNumber
  );

  return (
    <div className="min-h-screen bg-black pt-14">
      <div className="flex flex-col lg:flex-row">
        {/* Main content */}
        <div className="flex-1">
          {/* Top bar */}
          <div className="flex items-center justify-between px-4 py-3 bg-[#0a0a0a] border-b border-white/5">
            <div className="flex items-center gap-3 overflow-hidden">
              <Link
                href={`/movie/${movie.slug}`}
                className="flex items-center gap-1 text-sm text-[#b3b3b3] hover:text-white transition-colors shrink-0"
              >
                <ChevronLeft size={16} />
                <span className="hidden sm:inline">Quay lại</span>
              </Link>
              <span className="text-white font-semibold truncate">{movie.name}</span>
              {currentEpisode && (
                <span className="text-[#b3b3b3] text-sm shrink-0">
                  — {currentEpisode.name || `Tập ${episodeNumber}`}
                </span>
              )}
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <button
                onClick={handleShare}
                className="p-2 text-[#b3b3b3] hover:text-white transition-colors"
                title="Chia sẻ"
              >
                <Share2 size={16} />
              </button>
              <button
                onClick={() => setSidebarOpen(!sidebarOpen)}
                className={cn(
                  "p-2 transition-colors lg:hidden",
                  sidebarOpen ? "text-[#e50914]" : "text-[#b3b3b3] hover:text-white"
                )}
                title="Danh sách tập"
              >
                <List size={16} />
              </button>
            </div>
          </div>

          {/* Video player */}
          <div className="relative w-full bg-black" style={{ aspectRatio: "16/9" }}>
            {currentEpisode?.embedLink ? (
              <iframe
                src={currentEpisode.embedLink}
                className="absolute inset-0 w-full h-full"
                allowFullScreen
                allow="autoplay; fullscreen"
                title={`${movie.name} - Tập ${episodeNumber}`}
              />
            ) : currentEpisode?.fileUrl ? (
              <video
                src={currentEpisode.fileUrl}
                controls
                autoPlay
                className="absolute inset-0 w-full h-full"
              />
            ) : (
              <div className="absolute inset-0 flex flex-col items-center justify-center text-[#b3b3b3]">
                <AlertCircle size={48} className="mb-4 opacity-50" />
                <p className="text-sm">Không có nguồn video cho tập này.</p>
              </div>
            )}
          </div>

          {/* Episode navigation */}
          {sortedEpisodes.length > 1 && (
            <div className="flex items-center justify-between px-4 py-3 bg-[#0a0a0a]">
              <button
                onClick={prevEp}
                disabled={currentIdx <= 0}
                className="flex items-center gap-1.5 px-4 py-2 bg-[#1f1f1f] hover:bg-[#2a2a2a] text-white rounded-md text-sm disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
              >
                <ChevronLeft size={16} />
                Tập trước
              </button>
              <span className="text-sm text-[#b3b3b3]">
                {currentIdx + 1} / {sortedEpisodes.length}
              </span>
              <button
                onClick={nextEp}
                disabled={currentIdx >= sortedEpisodes.length - 1}
                className="flex items-center gap-1.5 px-4 py-2 bg-[#1f1f1f] hover:bg-[#2a2a2a] text-white rounded-md text-sm disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
              >
                Tập tiếp
                <ChevronRight size={16} />
              </button>
            </div>
          )}
        </div>

        {/* Sidebar — Episode list */}
        <aside
          className={cn(
            "lg:w-80 bg-[#0a0a0a] border-l border-white/5 overflow-y-auto transition-all duration-300",
            "lg:relative lg:block lg:shrink-0",
            sidebarOpen
              ? "block fixed inset-y-0 right-0 z-50 w-80 shadow-2xl"
              : "hidden"
          )}
        >
          {/* Movie info header */}
          <div className="sticky top-0 bg-[#0a0a0a] z-10 p-4 border-b border-white/5">
            <div className="flex gap-3">
              {posterUrl && (
                <div className="relative w-14 h-20 rounded overflow-hidden shrink-0 bg-[#1f1f1f]">
                  <Image
                    src={posterUrl}
                    alt={movie.name}
                    fill
                    className="object-cover"
                    sizes="56px"
                    unoptimized
                  />
                </div>
              )}
              <div className="min-w-0">
                <h3 className="text-sm font-semibold text-white line-clamp-2">
                  {movie.name}
                </h3>
                <p className="text-xs text-[#808080] mt-1">
                  {movie.year}
                  {movie.quality && ` • ${movie.quality}`}
                </p>
                <Link
                  href={`/movie/${movie.slug}`}
                  className="flex items-center gap-1 text-xs text-[#e50914] hover:underline mt-1"
                >
                  <Info size={12} />
                  Chi tiết
                </Link>
              </div>
            </div>
            <button
              onClick={() => setSidebarOpen(false)}
              className="lg:hidden absolute top-3 right-3 p-1 text-[#808080] hover:text-white"
            >
              <ChevronRight size={16} />
            </button>
          </div>

          {/* Episode list */}
          <div className="p-4">
            <h4 className="text-sm font-semibold text-white mb-3">Danh sách tập</h4>
            <EpisodeList
              episodes={sortedEpisodes}
              currentEp={episodeNumber}
              movieSlug={movie.slug}
            />
          </div>
        </aside>
      </div>
    </div>
  );
}
