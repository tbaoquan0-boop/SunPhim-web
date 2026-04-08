# HE-THONG-WEB-PHIM - Status

## Trang thai hoan thanh

| Phase | Trang thai | Chi tiet |
|-------|-----------|---------|
| 1 - Crawler | **HOAN THANH** | Ophim API, Quartz Jobs, Proxy Rotation, TMDB |
| 2 - Content Management | **HOAN THANH** | Sitemap, SEO DTO, SlugHelper |
| 3 - Streaming Proxy | **HOAN THANH** | Streaming Controller, M3U8 Proxy, GDrive, AntiHotlink |
| 4 - Cache & Performance | **HOAN THANH** | Redis Cache, Image Proxy |
| 5 - Ads & Tracking | **HOAN THANH** | AdSlot, Banner, FakeViewJob |
| 6 - Infrastructure | **HOAN THANH** | Docker Compose, Dockerfile, Caddyfile |
| 7 - Netflix UI Frontend | **HOAN THANH** | Homepage, Movie Detail, Player, Search, SPA routing |

## Cau truc file moi

```
SunPhim/
+--- Models/
|   +--- Crawler/OphimModels.cs      (DTO Ophim API)
|   +--- DTOs/DTOs.cs               (MovieListDto, MovieDetailDto, HomePageDto...)
|   +--- AdSlot.cs                   (AdSlot, Banner)
|
+--- Services/
|   +--- Crawler/
|   |   +--- IOphimService.cs        (Interface)
|   |   +--- OphimService.cs         (Crawl logic)
|   |   +--- ProxyService.cs         (Proxy rotation)
|   |   +--- TmdbService.cs          (TMDB enrichment)
|   +--- Streaming/
|   |   +--- AntiHotlinkService.cs   (HMAC token protection)
|   |   +--- GDriveService.cs        (GDrive bypass)
|   +--- Cache/
|   |   +--- CacheService.cs         (Redis + Memory fallback)
|   +--- AdService.cs                (Ad slot management)
|
+--- Controllers/
|   +--- HomeController.cs            (/api/home/* - Homepage data API)
|   +--- SitemapController.cs        (/sitemap.xml)
|   +--- StreamingController.cs       (/api/stream/*)
|   +--- ImageController.cs          (/api/image/proxy)
|   +--- CrawlerController.cs        (/api/crawler/*)
|   +--- AdsController.cs            (/api/ads/*)
|   +--- MoviesController.cs         (/api/movies/*)
|   +--- CategoriesController.cs    (/api/categories/*)
|   +--- EpisodesController.cs       (/api/episodes/*)
|
+--- Helpers/SlugHelper.cs           (Slug generator)
+--- Jobs/ScheduledJobs.cs           (Quartz jobs)
+--- docker-compose.yml               (Production infra)
+--- Dockerfile                       (Multi-stage build)
+--- Caddyfile                        (Reverse proxy + HTTPS)

+--- wwwroot/                        (Frontend Netflix UI)
    +--- index.html                   (Homepage)
    +--- movie.html                   (Movie detail page)
    +--- watch.html                   (Player page)
    +--- css/
    |   +--- netflix.css             (Complete Netflix-style CSS)
    +--- js/
    |   +--- api.js                  (API integration layer)
    |   +--- app.js                  (UI logic, components)
    +--- img/
        +--- placeholder.svg          (Image placeholder)
```

## Netflix UI Features

### Homepage
- Hero section with featured movie backdrop
- Horizontal scrollable movie rows (Trending, New Releases, Top Rated, Series, Singles)
- Category sections with dynamic movie lists
- Continue Watching section
- Global search overlay (Ctrl+K)
- Dark theme with Netflix-style design

### Movie Detail Page
- Large poster with backdrop image
- Movie metadata (IMDB score, year, duration, quality, language)
- Category tags with links
- Episode list grid with thumbnails
- Play button and info actions

### Player Page
- 16:9 responsive player container
- Episode navigation (previous/next)
- Episode mini-list below player
- Support for embed iframes and direct video (m3u8/mp4)
- Error state display

### API Endpoints (New)
- `GET /api/home` - Full homepage data
- `GET /api/home/featured` - Featured movies
- `GET /api/home/trending?limit=20` - Trending movies
- `GET /api/home/new-releases?limit=20` - New releases
- `GET /api/home/top-rated?limit=20` - Top rated movies
- `GET /api/home/random-featured?count=5` - Random featured
- `GET /api/home/by-category?limit=12` - Movies grouped by category
- `GET /api/home/hero-banner` - Hero banner
- `GET /api/home/continue-watching?userId=xxx` - Continue watching

## Lenh quan trọng

```powershell
# Chay migrations (user tu thuc hien)
dotnet ef migrations add AddAdSlotsAndBanners
dotnet ef database update

# Khoi dong Redis (Docker)
docker run -d -p 6379:6379 redis

# Chay production
docker-compose up -d

# Test crawl tay
POST /api/crawler/sync?page=1
POST /api/crawler/sync/all?maxPages=10
```

## Cau hinh quan trọng (appsettings.json)

```json
{
  "App:BaseUrl": "https://sunphim.example.com",
  "Redis:ConnectionString": "localhost:6379",
  "Streaming:HotlinkSecret": "change-this-to-random-secret",
  "TMDB:ApiKey": "your-tmdb-api-key",
  "Crawler:Proxies": ["http://proxy1:port", "http://proxy2:port"]
}
```

## API Endpoints moi

- `GET /sitemap.xml` - Sitemap cho Google SEO
- `GET /sitemap.xml/movies?page=1` - Sitemap phim (split page)
- `GET /sitemap.xml/ror.xml` - ROR sitemap
- `GET /api/stream/{videoId}?token=xxx` - Stream video (hotlink protected)
- `GET /api/stream/proxy?url=xxx` - Proxy m3u8
- `GET /api/stream/player/{episodeId}?proxy=true` - Lay embed URL
- `GET /api/stream/gdrive/resolve?url=xxx` - Resolve GDrive URL
- `POST /api/stream/report` - Bao cao link chet
- `GET /api/image/proxy?url=xxx&width=500` - Proxy hinh anh
- `POST /api/crawler/sync?page=1` - Crawl 1 trang
- `POST /api/crawler/sync/all?maxPages=10` - Crawl nhieu trang
- `POST /api/crawler/trigger/{jobName}` - Kich hoat job thủ cong
- `GET /api/ads/slots?url=/phim/xxx` - Lay ad slots cho trang
- `POST /api/ads/slots` - Tao ad slot
- `POST /api/ads/banners` - Tao banner
- `GET /api/home` - Homepage data (Featured, Trending, New Releases, Top Rated, Categories)
- `GET /api/home/featured` - Featured movies
- `GET /api/home/trending?limit=20` - Trending movies
- `GET /api/home/new-releases?limit=20` - New releases
- `GET /api/home/top-rated?limit=20` - Top rated movies
