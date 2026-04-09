// ============================================================
// SunPhim — Error Boundary
// ============================================================
"use client";

import { Component, type ReactNode } from "react";
import { AlertTriangle, RefreshCw } from "lucide-react";
import { cn } from "@/lib/utils";

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  className?: string;
}

interface State {
  hasError: boolean;
  message?: string;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, message: error.message };
  }

  reset = () => this.setState({ hasError: false, message: undefined });

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;
      return (
        <div className={cn("flex flex-col items-center justify-center py-20 px-4", this.props.className)}>
          <AlertTriangle className="w-12 h-12 text-yellow-500 mb-4" />
          <h3 className="text-lg font-semibold text-white mb-2">Đã xảy ra lỗi</h3>
          <p className="text-sm text-[#b3b3b3] mb-4 text-center max-w-md">
            {this.state.message || "Không thể tải nội dung. Vui lòng thử lại."}
          </p>
          <button
            onClick={this.reset}
            className="flex items-center gap-2 px-4 py-2 bg-[#e50914] text-white rounded-md hover:bg-[#b20710] transition-colors text-sm font-semibold"
          >
            <RefreshCw size={14} />
            Thử lại
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}

// ---------- Empty State Component ----------
export function EmptyState({
  message = "Chưa có dữ liệu",
  icon,
  className,
}: {
  message?: string;
  icon?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("flex flex-col items-center justify-center py-16 px-4", className)}>
      {icon || (
        <div className="w-16 h-16 rounded-full bg-[#1f1f1f] flex items-center justify-center mb-4">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#808080" strokeWidth="1.5">
            <rect x="2" y="2" width="20" height="20" rx="2" />
            <line x1="7" y1="12" x2="17" y2="12" />
          </svg>
        </div>
      )}
      <p className="text-[#b3b3b3] text-base">{message}</p>
    </div>
  );
}
