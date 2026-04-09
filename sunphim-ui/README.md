# SunPhim.UI — Frontend Next.js

Xây dựng lại giao diện web xem phim SunPhim bằng Next.js 15 (App Router) + Tailwind CSS.

## Yêu cầu

- Node.js 18+
- npm

## Cài đặt

```bash
cd sunphim-ui
npm install
```

## Chạy phát triển (Development)

```bash
npm run dev
```

Frontend chạy tại `http://localhost:3000`. API được proxy qua `/api/*` sang `https://sunphim.id.vn`.

## Build Production

```bash
npm run build
npm run start   # Chạy server production
```

## Deploy trên SmarterASP

### Phương án khuyên dùng: Chạy song song hai server

1. **Backend C#**: chạy ở port 5000/5001 (HTTPS)
2. **Frontend Next.js**: chạy ở port 3000
3. Dùng IIS URL Rewrite hoặc web.config để:
   - `/api/*` → Backend C# (port 5000)
   - `/*` → Frontend Next.js (port 3000)

**web.config cho backend C# (reverse proxy):**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <!-- Proxy /api/* sang C# backend -->
        <rule name="API to Backend" stopProcessing="true">
          <match url="^api/(.*)" />
          <action type="Rewrite" url="http://localhost:5000/api/{R:1}" />
        </rule>
        <!-- Proxy tất cả request còn lại sang Next.js -->
        <rule name="SPA to Next.js" stopProcessing="true">
          <match url="^(.*)" />
          <action type="Rewrite" url="http://localhost:3000/{R:1}" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

### Phương án Static Export (nếu SmarterASP không hỗ trợ Node.js)

1. Sửa `next.config.ts`, thêm `output: 'export'`
2. `npm run build` → thư mục `out/`
3. Copy `out/` vào `wwwroot/` của SmarterASP
4. Cấu hình ASP.NET Core fallback route (đã có trong Program.cs)

**Lưu ý**: Static export không hỗ trợ rewrites, nên cần set API URL trực tiếp:

```typescript
// lib/api.ts — sửa dòng đầu
const API_BASE = "https://sunphim.id.vn/api";
```

## JWT Secret (Backend C#)

Backend C# cần có trong `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "YourSecretKeyAtLeast32Characters!",
    "Issuer": "SunPhim"
  }
}
```

## Cấu hình ASP.NET Core — Database Migration

Sau khi thêm User model, chạy migration để tạo bảng:

```bash
cd SunPhim
dotnet ef migrations add AddUserAndUserRating
dotnet ef database update
```

## Cấu trúc thư mục

```
sunphim-ui/
├── app/                    # Next.js App Router
│   ├── layout.tsx         # Root layout (Header + Footer)
│   ├── page.tsx           # Trang chủ
│   ├── movie/[slug]/       # Chi tiết phim (dynamic SSR)
│   ├── watch/[slug]/       # Trang xem phim (dynamic SSR)
│   ├── browse/             # Duyệt phim theo thể loại
│   ├── search/             # Tìm kiếm
│   ├── category/[slug]/     # Phim theo thể loại (dynamic SSR)
│   └── auth/              # Login, Register, Profile
├── components/
│   ├── layout/             # Header, Footer
│   ├── home/              # HeroCarousel, MovieRow
│   ├── movie/              # MovieCard, EpisodeList, MovieDetailClient
│   ├── watch/              # WatchClient
│   └── ui/                 # Skeleton, Toast, ErrorBoundary
├── lib/
│   ├── api.ts              # Typed API client
│   ├── store.ts            # Zustand auth store
│   └── utils.ts            # Hàm tiện ích
└── types/
    └── index.ts            # TypeScript interfaces
```

## Tính năng đã hoàn thành

- [x] Dark Netflix-style UI với Tailwind CSS
- [x] Hero Carousel với auto-play, touch swipe, progress bar
- [x] MovieRow — horizontal scroll carousel với prev/next buttons
- [x] Trang chi tiết phim với poster, rating, episode list
- [x] Trang xem phim với sidebar episode list, prev/next navigation
- [x] Browse page với filter type + sort
- [x] Search page với debounce
- [x] Authentication (Login, Register, Profile)
- [x] Lịch sử xem (Watch History)
- [x] User rating (1-10)
- [x] ErrorBoundary — không crash khi API lỗi
- [x] Empty state — "Chưa có dữ liệu"
- [x] Skeleton loading
- [x] Responsive (mobile + desktop)
- [x] SEO metadata cho trang chi tiết phim
- [x] JWT Authentication backend (AuthController + UserService)
- [x] User + UserRating models với EF Core migration
