// ============================================================
// SunPhim — Header Component
// ============================================================
"use client";

import Link from "next/link";
import { useState, useEffect, useRef } from "react";
import { Search, Menu, X, User, LogOut, Film, ChevronDown } from "lucide-react";
import { useAuthStore } from "@/lib/store";
import { cn } from "@/lib/utils";
import { useRouter } from "next/navigation";

const NAV_LINKS = [
  { href: "/", label: "Trang chủ" },
  { href: "/browse?type=series", label: "Phim bộ" },
  { href: "/browse?type=single", label: "Phim lẻ" },
  { href: "/browse", label: "Duyệt phim" },
];

export function Header() {
  const router = useRouter();
  const { isAuthenticated, user, clearAuth } = useAuthStore();
  const [scrolled, setScrolled] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const userMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 50);
    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, []);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      router.push(`/search?q=${encodeURIComponent(searchQuery.trim())}`);
      setSearchOpen(false);
      setSearchQuery("");
    }
  };

  const handleLogout = () => {
    clearAuth();
    setUserMenuOpen(false);
    router.push("/");
  };

  return (
    <header
      className={cn(
        "fixed top-0 left-0 right-0 z-50 transition-all duration-300",
        scrolled
          ? "bg-[#141414]/95 backdrop-blur-md shadow-lg"
          : "bg-gradient-to-b from-black/80 to-transparent"
      )}
    >
      <div className="max-w-[1400px] mx-auto px-[4%] h-14 flex items-center justify-between">
        {/* Left: Logo + Nav */}
        <div className="flex items-center gap-6">
          <Link href="/" className="flex items-center gap-2 shrink-0">
            <Film className="w-7 h-7 text-[#e50914]" />
            <span className="text-xl font-bold tracking-tight text-white">
              Sun<span className="text-[#e50914]">Phim</span>
            </span>
          </Link>

          <nav className="hidden md:flex items-center gap-5">
            {NAV_LINKS.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className="text-sm font-medium text-[#b3b3b3] hover:text-white transition-colors relative group"
              >
                {link.label}
                <span className="absolute -bottom-0.5 left-0 w-0 h-0.5 bg-[#e50914] group-hover:w-full transition-all duration-200" />
              </Link>
            ))}
          </nav>
        </div>

        {/* Right: Search + Auth */}
        <div className="flex items-center gap-2">
          {/* Search */}
          <div className="relative">
            {searchOpen ? (
              <form onSubmit={handleSearch} className="flex items-center">
                <input
                  autoFocus
                  type="text"
                  placeholder="Tìm phim..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-40 sm:w-56 h-9 bg-[#1f1f1f] border border-white/20 rounded-md px-3 text-sm text-white placeholder-[#808080] focus:outline-none focus:border-[#e50914] transition-colors"
                />
                <button
                  type="button"
                  onClick={() => { setSearchOpen(false); setSearchQuery(""); }}
                  className="ml-1 p-2 text-[#b3b3b3] hover:text-white"
                >
                  <X size={16} />
                </button>
              </form>
            ) : (
              <button
                onClick={() => setSearchOpen(true)}
                className="p-2 text-[#b3b3b3] hover:text-white transition-colors"
                aria-label="Tìm kiếm"
              >
                <Search size={20} />
              </button>
            )}
          </div>

          {/* User Menu */}
          {isAuthenticated ? (
            <div className="relative" ref={userMenuRef}>
              <button
                onClick={() => setUserMenuOpen(!userMenuOpen)}
                className="flex items-center gap-1.5 p-1.5 rounded-md hover:bg-white/10 transition-colors"
              >
                <div className="w-7 h-7 rounded-full bg-[#e50914] flex items-center justify-center text-xs font-bold text-white">
                  {user?.username?.charAt(0).toUpperCase() || "U"}
                </div>
                <ChevronDown size={14} className="text-[#b3b3b3]" />
              </button>
              {userMenuOpen && (
                <div className="absolute right-0 top-full mt-1 w-48 bg-[#1f1f1f] border border-white/10 rounded-lg shadow-xl overflow-hidden">
                  <div className="px-3 py-2 border-b border-white/10">
                    <p className="text-sm font-medium text-white truncate">{user?.username}</p>
                    <p className="text-xs text-[#808080] truncate">{user?.email}</p>
                  </div>
                  <Link
                    href="/profile"
                    onClick={() => setUserMenuOpen(false)}
                    className="flex items-center gap-2 px-3 py-2.5 text-sm text-[#b3b3b3] hover:bg-white/10 hover:text-white transition-colors"
                  >
                    <User size={14} /> Hồ sơ
                  </Link>
                  <button
                    onClick={handleLogout}
                    className="w-full flex items-center gap-2 px-3 py-2.5 text-sm text-[#b3b3b3] hover:bg-white/10 hover:text-white transition-colors"
                  >
                    <LogOut size={14} /> Đăng xuất
                  </button>
                </div>
              )}
            </div>
          ) : (
            <div className="hidden sm:flex items-center gap-2">
              <Link href="/auth/login" className="text-sm font-medium text-[#b3b3b3] hover:text-white transition-colors px-2 py-1.5">
                Đăng nhập
              </Link>
              <Link href="/auth/register" className="text-sm font-semibold bg-[#e50914] text-white px-4 py-1.5 rounded-md hover:bg-[#b20710] transition-colors">
                Đăng ký
              </Link>
            </div>
          )}

          {/* Mobile hamburger */}
          <button
            onClick={() => setMobileOpen(!mobileOpen)}
            className="md:hidden p-2 text-[#b3b3b3] hover:text-white"
            aria-label="Menu"
          >
            {mobileOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>
      </div>

      {/* Mobile Nav */}
      {mobileOpen && (
        <div className="md:hidden bg-[#141414] border-t border-white/10 px-[4%] py-4 space-y-1">
          {NAV_LINKS.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              onClick={() => setMobileOpen(false)}
              className="block py-2.5 text-sm font-medium text-[#b3b3b3] hover:text-white transition-colors"
            >
              {link.label}
            </Link>
          ))}
          {!isAuthenticated && (
            <div className="pt-3 border-t border-white/10 space-y-2">
              <Link href="/auth/login" onClick={() => setMobileOpen(false)} className="block text-sm text-[#b3b3b3] hover:text-white py-1.5">
                Đăng nhập
              </Link>
              <Link href="/auth/register" onClick={() => setMobileOpen(false)} className="block text-sm font-semibold bg-[#e50914] text-white px-4 py-2 rounded-md text-center w-fit">
                Đăng ký
              </Link>
            </div>
          )}
        </div>
      )}
    </header>
  );
}
