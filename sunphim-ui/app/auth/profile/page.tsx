// ============================================================
// SunPhim — Profile Page
// ============================================================
"use client";

import { useEffect } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { User, Clock, Film, LogOut, Edit2 } from "lucide-react";
import { useAuthStore } from "@/lib/store";
import { getWatchHistory } from "@/lib/api";
import { buildImageUrl, formatDate } from "@/lib/utils";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/ErrorBoundary";

export default function ProfilePage() {
  const router = useRouter();
  const { user, isAuthenticated, watchHistory, setHistory, clearAuth } = useAuthStore();

  // Redirect if not logged in
  useEffect(() => {
    if (!isAuthenticated) {
      router.push("/auth/login");
    }
  }, [isAuthenticated, router]);

  const { data: history, isLoading } = useQuery({
    queryKey: ["watch-history"],
    queryFn: getWatchHistory,
    enabled: isAuthenticated,
  });

  useEffect(() => {
    if (history) setHistory(history);
  }, [history, setHistory]);

  const handleLogout = () => {
    clearAuth();
    router.push("/");
  };

  if (!isAuthenticated) return null;

  return (
    <div className="min-h-screen pt-14 pb-12">
      {/* Header */}
      <div className="bg-[#0a0a0a] border-b border-white/5 px-[4%] py-8">
        <div className="max-w-[1400px] mx-auto">
          <div className="flex flex-col sm:flex-row items-center gap-6">
            {/* Avatar */}
            <div className="w-20 h-20 rounded-full bg-[#e50914] flex items-center justify-center text-3xl font-bold text-white shrink-0">
              {user?.username?.charAt(0).toUpperCase() || "U"}
            </div>
            <div className="text-center sm:text-left flex-1">
              <h1 className="text-2xl font-bold text-white">{user?.username}</h1>
              <p className="text-sm text-[#808080]">{user?.email}</p>
              <p className="text-xs text-[#808080] mt-1">
                Tham gia: {user?.createdAt ? formatDate(user.createdAt) : "N/A"}
              </p>
            </div>
            <button
              onClick={handleLogout}
              className="flex items-center gap-2 px-4 py-2 bg-[#1f1f1f] hover:bg-[#2a2a2a] border border-white/10 text-[#b3b3b3] hover:text-white rounded-md text-sm transition-colors"
            >
              <LogOut size={14} />
              Đăng xuất
            </button>
          </div>
        </div>
      </div>

      <div className="max-w-[1400px] mx-auto px-[4%] py-8">
        {/* Stats */}
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 mb-10">
          <div className="bg-[#1f1f1f] rounded-lg p-4 border border-white/5">
            <div className="flex items-center gap-2 mb-2">
              <Clock size={16} className="text-[#e50914]" />
              <span className="text-xs text-[#808080]">Lịch sử xem</span>
            </div>
            <p className="text-2xl font-bold text-white">
              {watchHistory.length > 0 ? watchHistory.length : (isLoading ? "..." : "0")}
            </p>
          </div>
          <div className="bg-[#1f1f1f] rounded-lg p-4 border border-white/5">
            <div className="flex items-center gap-2 mb-2">
              <Film size={16} className="text-[#e50914]" />
              <span className="text-xs text-[#808080]">Phim đã xem</span>
            </div>
            <p className="text-2xl font-bold text-white">
              {watchHistory.length > 0
                ? new Set(watchHistory.map((h) => h.movieId)).size
                : "0"}
            </p>
          </div>
          <div className="bg-[#1f1f1f] rounded-lg p-4 border border-white/5">
            <div className="flex items-center gap-2 mb-2">
              <User size={16} className="text-[#e50914]" />
              <span className="text-xs text-[#808080]">Thành viên</span>
            </div>
            <p className="text-2xl font-bold text-white">Pro</p>
          </div>
        </div>

        {/* Watch history */}
        <div>
          <h2 className="text-lg font-semibold text-white mb-4 flex items-center gap-2">
            <Clock size={18} className="text-[#e50914]" />
            Lịch sử xem gần đây
          </h2>

          {isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="flex gap-3">
                  <Skeleton className="w-16 h-24 rounded-md shrink-0" />
                  <div className="flex-1 space-y-2">
                    <Skeleton className="h-4 w-1/2" />
                    <Skeleton className="h-3 w-1/3" />
                    <Skeleton className="h-3 w-1/4" />
                  </div>
                </div>
              ))}
            </div>
          ) : watchHistory.length === 0 ? (
            <EmptyState message="Bạn chưa xem phim nào." className="py-12" />
          ) : (
            <div className="space-y-2">
              {watchHistory.map((item, idx) => (
                <Link
                  key={`${item.movieId}-${item.episodeNumber}-${idx}`}
                  href={`/watch/${item.movieSlug}?ep=${item.episodeNumber}`}
                  className="flex gap-3 p-3 rounded-lg hover:bg-[#1f1f1f] transition-colors group"
                >
                  {/* Poster */}
                  <div className="relative w-16 h-24 rounded-md overflow-hidden shrink-0 bg-[#1f1f1f]">
                    {item.moviePoster ? (
                      <Image
                        src={buildImageUrl(item.moviePoster, false)}
                        alt={item.movieName}
                        fill
                        className="object-cover"
                        sizes="64px"
                        unoptimized
                      />
                    ) : (
                      <div className="absolute inset-0 flex items-center justify-center">
                        <Film size={20} className="text-[#2a2a2a]" />
                      </div>
                    )}
                  </div>

                  {/* Info */}
                  <div className="flex-1 min-w-0 py-0.5">
                    <p className="text-sm font-medium text-white group-hover:text-[#e50914] transition-colors line-clamp-1">
                      {item.movieName}
                    </p>
                    <p className="text-xs text-[#808080] mt-0.5">
                      Tập {item.episodeNumber}
                    </p>
                    <p className="text-xs text-[#808080] mt-0.5">
                      {item.watchedAt ? formatDate(item.watchedAt) : ""}
                    </p>
                  </div>

                  <div className="flex items-center">
                    <span className="text-xs px-2 py-1 bg-[#e50914] text-white rounded text-xs font-semibold">
                      Xem lại
                    </span>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
