// ============================================================
// SunPhim — Footer Component
// ============================================================
import Link from "next/link";
import { Film } from "lucide-react";

const FOOTER_LINKS = {
  "Danh mục": [
    { href: "/browse?type=series", label: "Phim bộ" },
    { href: "/browse?type=single", label: "Phim lẻ" },
    { href: "/browse", label: "Duyệt phim" },
  ],
  "Hỗ trợ": [
    { href: "/about", label: "Giới thiệu" },
    { href: "/contact", label: "Liên hệ" },
    { href: "/terms", label: "Điều khoản sử dụng" },
    { href: "/privacy", label: "Chính sách bảo mật" },
  ],
};

export function Footer() {
  return (
    <footer className="bg-black border-t border-white/5 mt-auto">
      <div className="max-w-[1400px] mx-auto px-[4%] py-12">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8 mb-8">
          {/* Brand */}
          <div>
            <Link href="/" className="flex items-center gap-2 mb-3">
              <Film className="w-6 h-6 text-[#e50914]" />
              <span className="text-lg font-bold text-white">
                Sun<span className="text-[#e50914]">Phim</span>
              </span>
            </Link>
            <p className="text-sm text-[#808080] leading-relaxed">
              Xem phim chất lượng cao, cập nhật nhanh chóng với giao diện hiện đại và trải nghiệm mượt mà.
            </p>
            <div className="flex items-center gap-3 mt-4">
              <a href="#" className="text-[#808080] hover:text-white transition-colors" aria-label="GitHub">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z"/></svg>
              </a>
              <a href="#" className="text-[#808080] hover:text-white transition-colors" aria-label="Email">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="2" y="4" width="20" height="16" rx="2"/><path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/></svg>
              </a>
            </div>
          </div>

          {/* Links */}
          {Object.entries(FOOTER_LINKS).map(([title, links]) => (
            <div key={title}>
              <h4 className="text-sm font-semibold text-white mb-3">{title}</h4>
              <ul className="space-y-2">
                {links.map((link) => (
                  <li key={link.href}>
                    <Link
                      href={link.href}
                      className="text-sm text-[#808080] hover:text-white transition-colors"
                    >
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}

          {/* Contact */}
          <div>
            <h4 className="text-sm font-semibold text-white mb-3">Liên hệ</h4>
            <p className="text-sm text-[#808080] leading-relaxed">
              Email: support@sunphim.id.vn
            </p>
            <p className="text-sm text-[#808080] mt-1">
              Website: sunphim.id.vn
            </p>
          </div>
        </div>

        <div className="border-t border-white/5 pt-6 text-center">
          <p className="text-xs text-[#808080]">
            © {new Date().getFullYear()} SunPhim. Mọi nội dung được cung cấp từ nguồn công khai.
          </p>
        </div>
      </div>
    </footer>
  );
}
