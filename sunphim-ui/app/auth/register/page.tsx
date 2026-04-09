// ============================================================
// SunPhim — Register Page
// ============================================================
"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Film, Eye, EyeOff, AlertCircle, CheckCircle } from "lucide-react";
import { register } from "@/lib/api";
import { useAuthStore } from "@/lib/store";
import { useToastStore } from "@/components/ui/Toast";

export default function RegisterPage() {
  const router = useRouter();
  const { setAuth } = useAuthStore();
  const { addToast } = useToastStore();
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPw, setConfirmPw] = useState("");
  const [showPw, setShowPw] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  const passwordStrong = password.length >= 8;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    if (!username.trim() || !email.trim() || !password || !confirmPw) {
      setError("Vui lòng nhập đầy đủ thông tin.");
      return;
    }
    if (!passwordStrong) {
      setError("Mật khẩu phải có ít nhất 8 ký tự.");
      return;
    }
    if (password !== confirmPw) {
      setError("Mật khẩu xác nhận không khớp.");
      return;
    }
    setIsLoading(true);
    try {
      const res = await register({ email: email.trim(), password, username: username.trim() });
      setAuth(res.user, res.token);
      addToast("Đăng ký thành công!", "success");
      router.push("/");
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Đăng ký thất bại. Vui lòng thử lại."
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#141414] pt-14 px-4">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="flex flex-col items-center mb-8">
          <Link href="/" className="flex items-center gap-2 mb-4">
            <Film className="w-9 h-9 text-[#e50914]" />
            <span className="text-2xl font-bold text-white">
              Sun<span className="text-[#e50914]">Phim</span>
            </span>
          </Link>
          <h1 className="text-2xl font-bold text-white">Tạo tài khoản</h1>
          <p className="text-sm text-[#808080] mt-1">
            Đăng ký miễn phí để trải nghiệm đầy đủ tính năng.
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="flex items-center gap-2 p-3 bg-red-900/30 border border-red-500/30 rounded-md text-sm text-red-400">
              <AlertCircle size={16} />
              {error}
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-[#b3b3b3] mb-1.5">
              Tên người dùng
            </label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Tên của bạn"
              className="w-full h-11 px-4 bg-[#1f1f1f] border border-white/10 rounded-md text-white placeholder-[#808080] focus:outline-none focus:border-[#e50914] transition-colors text-sm"
              autoComplete="username"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-[#b3b3b3] mb-1.5">
              Email
            </label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="email@example.com"
              className="w-full h-11 px-4 bg-[#1f1f1f] border border-white/10 rounded-md text-white placeholder-[#808080] focus:outline-none focus:border-[#e50914] transition-colors text-sm"
              autoComplete="email"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-[#b3b3b3] mb-1.5">
              Mật khẩu
            </label>
            <div className="relative">
              <input
                type={showPw ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Ít nhất 8 ký tự"
                className="w-full h-11 px-4 pr-11 bg-[#1f1f1f] border border-white/10 rounded-md text-white placeholder-[#808080] focus:outline-none focus:border-[#e50914] transition-colors text-sm"
                autoComplete="new-password"
              />
              <button
                type="button"
                onClick={() => setShowPw(!showPw)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-[#808080] hover:text-white transition-colors"
              >
                {showPw ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
            {password && (
              <div className="flex items-center gap-1.5 mt-1.5">
                {passwordStrong ? (
                  <CheckCircle size={12} className="text-green-500" />
                ) : (
                  <AlertCircle size={12} className="text-yellow-500" />
                )}
                <span className={`text-xs ${passwordStrong ? "text-green-500" : "text-yellow-500"}`}>
                  {passwordStrong ? "Mật khẩu hợp lệ" : "Ít nhất 8 ký tự"}
                </span>
              </div>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-[#b3b3b3] mb-1.5">
              Xác nhận mật khẩu
            </label>
            <input
              type="password"
              value={confirmPw}
              onChange={(e) => setConfirmPw(e.target.value)}
              placeholder="Nhập lại mật khẩu"
              className="w-full h-11 px-4 bg-[#1f1f1f] border border-white/10 rounded-md text-white placeholder-[#808080] focus:outline-none focus:border-[#e50914] transition-colors text-sm"
              autoComplete="new-password"
            />
          </div>

          <button
            type="submit"
            disabled={isLoading}
            className="w-full h-11 bg-[#e50914] hover:bg-[#b20710] disabled:opacity-50 disabled:cursor-not-allowed text-white font-semibold rounded-md transition-colors text-sm"
          >
            {isLoading ? "Đang đăng ký..." : "Tạo tài khoản"}
          </button>
        </form>

        <p className="text-center text-sm text-[#808080] mt-6">
          Đã có tài khoản?{" "}
          <Link href="/auth/login" className="text-[#e50914] hover:underline font-medium">
            Đăng nhập
          </Link>
        </p>
      </div>
    </div>
  );
}
