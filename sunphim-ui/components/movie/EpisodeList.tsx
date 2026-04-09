// ============================================================
// SunPhim — Episode List Component
// ============================================================
"use client";

import Link from "next/link";
import { cn } from "@/lib/utils";
import type { EpisodeDto } from "@/types";

interface EpisodeListProps {
  episodes: EpisodeDto[];
  currentEp?: number;
  movieSlug: string;
}

export function EpisodeList({ episodes, currentEp, movieSlug }: EpisodeListProps) {
  if (!episodes || episodes.length === 0) {
    return (
      <p className="text-sm text-[#808080] py-4">Chưa có tập phim nào.</p>
    );
  }

  // Sort by episode number
  const sorted = [...episodes].sort(
    (a, b) => a.episodeNumber - b.episodeNumber
  );

  return (
    <div className="grid grid-cols-4 sm:grid-cols-6 md:grid-cols-8 lg:grid-cols-10 gap-2">
      {sorted.map((ep) => {
        const isActive = ep.episodeNumber === currentEp;
        return (
          <Link
            key={ep.id}
            href={`/watch/${movieSlug}?ep=${ep.episodeNumber}`}
            className={cn(
              "flex items-center justify-center px-2 py-2.5 rounded-md text-sm font-medium transition-all duration-150",
              "border",
              isActive
                ? "bg-[#e50914] border-[#e50914] text-white"
                : "bg-[#1f1f1f] border-white/10 text-[#b3b3b3] hover:bg-[#2a2a2a] hover:text-white hover:border-white/20"
            )}
          >
            {ep.name || `Tập ${ep.episodeNumber}`}
          </Link>
        );
      })}
    </div>
  );
}
