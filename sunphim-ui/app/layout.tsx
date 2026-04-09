import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Providers } from "@/components/Providers";
import { Header } from "@/components/layout/Header";
import { Footer } from "@/components/layout/Footer";
import { ToastContainer } from "@/components/ui/Toast";

const inter = Inter({ subsets: ["latin"], variable: "--font-inter" });

export const metadata: Metadata = {
  title: {
    default: "SunPhim - Xem Phim Chất Lượng Cao",
    template: "%s | SunPhim",
  },
  description: "Xem phim chất lượng cao, cập nhật nhanh chóng, giao diện hiện đại.",
  keywords: ["phim", "xem phim", "phim online", "phim vietsub", "sunphim"],
  openGraph: {
    type: "website",
    siteName: "SunPhim",
    title: "SunPhim - Xem Phim Chất Lượng Cao",
    description: "Xem phim chất lượng cao, cập nhật nhanh chóng.",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" className={inter.className}>
      <body className="min-h-screen flex flex-col bg-[#141414] text-white">
        <Providers>
          <Header />
          <main className="flex-1 pt-14">{children}</main>
          <Footer />
          <ToastContainer />
        </Providers>
      </body>
    </html>
  );
}
