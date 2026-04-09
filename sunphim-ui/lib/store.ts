// ============================================================
// SunPhim — Zustand Auth Store
// ============================================================
import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { User, WatchHistoryItem } from "@/types";

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  watchHistory: WatchHistoryItem[];
  setAuth: (user: User, token: string) => void;
  clearAuth: () => void;
  updateUser: (user: Partial<User>) => void;
  addToHistory: (item: WatchHistoryItem) => void;
  setHistory: (items: WatchHistoryItem[]) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      isAuthenticated: false,
      watchHistory: [],

      setAuth: (user, token) =>
        set({ user, token, isAuthenticated: true }),

      clearAuth: () =>
        set({ user: null, token: null, isAuthenticated: false }),

      updateUser: (partial) =>
        set((state) => ({
          user: state.user ? { ...state.user, ...partial } : null,
        })),

      addToHistory: (item) =>
        set((state) => {
          const filtered = state.watchHistory.filter(
            (h) => !(h.movieId === item.movieId && h.episodeNumber === item.episodeNumber)
          );
          return {
            watchHistory: [item, ...filtered].slice(0, 50),
          };
        }),

      setHistory: (items) => set({ watchHistory: items }),
    }),
    {
      name: "sunphim-auth",
    }
  )
);
