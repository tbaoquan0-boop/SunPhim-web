using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SunPhim.Data;
using SunPhim.Helpers;
using SunPhim.Models;
using SunPhim.Models.Crawler;

namespace SunPhim.Services.Crawler;

public interface INguonCService
{
    Task<CrawlResult> CrawlMovieDetailAsync(string slug, CancellationToken ct = default);
    Task<CrawlResult> CrawlListPageAsync(string url, CancellationToken ct = default);
    Task<CrawlResult> CrawlNewUpdatesAsync(int page = 1, CancellationToken ct = default);
    Task<CrawlResult> CrawlMovieListAsync(int page = 1, CancellationToken ct = default);
    Task<int> SyncAllPagesAsync(int maxPages = 10, CancellationToken ct = default);
    Task<int> SyncPagesAsync(int fromPage, int toPage, CancellationToken ct = default);
    Task<CrawlResult> CrawlPagesWithDelayAsync(int fromPage = 1, int toPage = 15, CancellationToken ct = default);
    Task<CrawlResult> CrawlDanhSachAsync(string listSlug, int? maxPages = null, CancellationToken ct = default);
    Task<CrawlResult> CrawlFullDanhSachAsync(string listSlug, CancellationToken ct = default);
    Task<CrawlResult> CrawlNamPhatHanhAsync(int year, int? maxPages = null, CancellationToken ct = default);
    Task<CrawlResult> CrawlAllYearsAsync(int minYear, int maxYear, int? maxPagesPerYear = null, CancellationToken ct = default);
    Task<CrawlResult> CrawlTheLoaiAsync(string slug, int? maxPages = null, CancellationToken ct = default);
    Task<CrawlResult> CrawlQuocGiaAsync(string slug, int? maxPages = null, CancellationToken ct = default);
    Task<CrawlResult> CrawlSearchAsync(string keyword, int? maxPages = null, CancellationToken ct = default);
}

public class NguonCService : INguonCService
{
    private readonly HttpClient _http;
    private readonly ILogger<NguonCService> _log;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private static readonly JsonSerializerOptions DetailJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions ListJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public NguonCService(HttpClient http, ILogger<NguonCService> log, IServiceScopeFactory scopeFactory)
    {
        _http = http;
        _log = log;
        _scopeFactory = scopeFactory;
        _http.Timeout = TimeSpan.FromSeconds(120);
        ApplyBrowserHeaders(_http);
    }

    private static void ApplyBrowserHeaders(HttpClient http)
    {
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ChromeUserAgent);
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
    }

    public Task<CrawlResult> CrawlMovieListAsync(int page = 1, CancellationToken ct = default)
        => CrawlNewUpdatesAsync(page, ct);

    public async Task<CrawlResult> CrawlNewUpdatesAsync(int page = 1, CancellationToken ct = default)
    {
        try
        {
            var url = NguonCApiPaths.BuildListUrl(NguonCApiPaths.PhimMoiCapNhat, page, null);
            var list = await FetchFilmsListAsync(url, ct);
            if (list == null || list.Status != "success" || list.Items.Count == 0)
                return CrawlResult.Fail($"Khong lay duoc danh sach phim moi cap nhat (trang {page}).");

            // false: luon goi API chi tiet — cap nhat thumb/poster/tap (skip true lam DB cu, anh trung/sai lau).
            var (n, u, e) = await ProcessSummarySlugsAsync(list.Items, skipIfExists: false, ct);
            _log.LogInformation("Phim moi cap nhat trang {Page}: {New} moi, {Upd} cap nhat, {Ep} tap", page, n, u, e);
            return CrawlResult.Ok(newMovies: n, updated: u, episodes: e,
                msg: $"Trang {page}: {n} moi, {u} cap nhat, {e} tap");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(ex, "CrawlNewUpdatesAsync trang {Page}", page);
            return CrawlResult.Fail(ex.Message);
        }
    }

    public async Task<int> SyncAllPagesAsync(int maxPages = 10, CancellationToken ct = default)
    {
        int synced = 0;
        int lastPageToFetch = maxPages;

        for (int page = 1; page <= lastPageToFetch; page++)
        {
            ct.ThrowIfCancellationRequested();
            var url = NguonCApiPaths.BuildListUrl(NguonCApiPaths.PhimMoiCapNhat, page, null);
            var list = await FetchFilmsListAsync(url, ct);
            if (list == null || list.Status != "success" || list.Items.Count == 0)
            {
                _log.LogInformation("SyncAllPages: het du lieu tai trang {Page}", page);
                break;
            }

            if (page == 1 && list.Paginate != null)
                lastPageToFetch = Math.Min(list.Paginate.TotalPage, maxPages);

            Console.WriteLine($"\n[DOWN] Trang phim moi {page}/{lastPageToFetch} ({list.Items.Count} phim)...");
            var (n, u, e) = await ProcessSummarySlugsAsync(list.Items, skipIfExists: true, ct);
            Console.WriteLine($"[OK] Trang {page}: {n} moi, {u} cap nhat, {e} tap");
            synced++;
            await Task.Delay(600, ct);
        }

        return synced;
    }

    public async Task<int> SyncPagesAsync(int fromPage, int toPage, CancellationToken ct = default)
    {
        if (toPage < fromPage) return 0;
        int synced = 0;
        for (int page = fromPage; page <= toPage; page++)
        {
            ct.ThrowIfCancellationRequested();
            var url = NguonCApiPaths.BuildListUrl(NguonCApiPaths.PhimMoiCapNhat, page, null);
            var list = await FetchFilmsListAsync(url, ct);
            if (list == null || list.Status != "success" || list.Items.Count == 0)
            {
                _log.LogInformation("SyncPagesAsync: het du lieu tai trang {Page}", page);
                break;
            }

            Console.WriteLine($"\n[DOWN] Dong bo trang {page} ({list.Items.Count} phim)...");
            var (n, u, e) = await ProcessSummarySlugsAsync(list.Items, skipIfExists: true, ct);
            Console.WriteLine($"[OK] Trang {page}: {n} moi, {u} cap nhat, {e} tap");
            synced++;
            await Task.Delay(600, ct);
        }

        return synced;
    }

    public async Task<CrawlResult> CrawlPagesWithDelayAsync(int fromPage = 1, int toPage = 15, CancellationToken ct = default)
    {
        if (toPage < fromPage)
            return CrawlResult.Fail("toPage phai >= fromPage");

        int synced = 0;
        for (int page = fromPage; page <= toPage; page++)
        {
            ct.ThrowIfCancellationRequested();
            var r = await CrawlNewUpdatesAsync(page, ct);
            if (!r.Success)
                return CrawlResult.Fail(r.Errors.FirstOrDefault() ?? r.Message ?? "Loi crawl");
            synced++;
            await Task.Delay(600, ct);
        }

        return CrawlResult.Ok(msg: $"Da cai {synced} trang (tu {fromPage} den {toPage})");
    }

    public Task<CrawlResult> CrawlDanhSachAsync(string listSlug, int? maxPages = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(listSlug))
            return Task.FromResult(CrawlResult.Fail("listSlug trong"));
        return CrawlPagedFilmListAsync(NguonCApiPaths.DanhSach(listSlug), null, maxPages, $"danh-sach/{listSlug.Trim()}", ct);
    }

    public Task<CrawlResult> CrawlFullDanhSachAsync(string endpoint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return Task.FromResult(CrawlResult.Fail("endpoint trong"));
        return CrawlPagedFilmListAsync(endpoint, null, null, $"FULL/{endpoint.Trim()}", ct);
    }

    public Task<CrawlResult> CrawlNamPhatHanhAsync(int year, int? maxPages = null, CancellationToken ct = default)
        => CrawlPagedFilmListAsync(NguonCApiPaths.NamPhatHanh(year), null, maxPages, $"nam {year}", ct);

    public async Task<CrawlResult> CrawlAllYearsAsync(int minYear, int maxYear, int? maxPagesPerYear = null, CancellationToken ct = default)
    {
        if (maxYear < minYear)
            return CrawlResult.Fail("maxYear phai >= minYear");

        int totalNew = 0, totalUpd = 0, totalEp = 0;
        for (int year = maxYear; year >= minYear; year--)
        {
            ct.ThrowIfCancellationRequested();
            Console.WriteLine($"\n========== Nam phat hanh: {year} ==========");
            var r = await CrawlNamPhatHanhAsync(year, maxPagesPerYear, ct);
            if (!r.Success)
            {
                _log.LogWarning("Nam {Year}: {Err}", year, r.Errors.FirstOrDefault() ?? r.Message);
                continue;
            }

            totalNew += r.NewMovies;
            totalUpd += r.UpdatedMovies;
            totalEp += r.EpisodesProcessed;
            await Task.Delay(500, ct);
        }

        return CrawlResult.Ok(newMovies: totalNew, updated: totalUpd, episodes: totalEp,
            msg: $"Tat ca nam {minYear}-{maxYear}: {totalNew} moi, {totalUpd} cap nhat, {totalEp} tap");
    }

    public Task<CrawlResult> CrawlTheLoaiAsync(string slug, int? maxPages = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.FromResult(CrawlResult.Fail("slug the loai trong"));
        return CrawlPagedFilmListAsync(NguonCApiPaths.TheLoai(slug), null, maxPages, $"the-loai/{slug.Trim()}", ct);
    }

    public Task<CrawlResult> CrawlQuocGiaAsync(string slug, int? maxPages = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.FromResult(CrawlResult.Fail("slug quoc gia trong"));
        return CrawlPagedFilmListAsync(NguonCApiPaths.QuocGia(slug), null, maxPages, $"quoc-gia/{slug.Trim()}", ct);
    }

    public Task<CrawlResult> CrawlSearchAsync(string keyword, int? maxPages = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Task.FromResult(CrawlResult.Fail("keyword trong"));
        var q = $"keyword={Uri.EscapeDataString(keyword.Trim())}";
        return CrawlPagedFilmListAsync(NguonCApiPaths.Search, q, maxPages, $"search '{keyword.Trim()}'", ct);
    }

    private async Task<CrawlResult> CrawlPagedFilmListAsync(
        string relativePath,
        string? extraQuery,
        int? maxPagesCap,
        string logLabel,
        CancellationToken ct)
    {
        try
        {
            int newCount = 0, updCount = 0, epCount = 0;
            int page = 1;
            int lastPage = maxPagesCap ?? int.MaxValue;

            while (page <= lastPage)
            {
                ct.ThrowIfCancellationRequested();
                var url = NguonCApiPaths.BuildListUrl(relativePath, page, extraQuery);
                var list = await FetchFilmsListAsync(url, ct);
                if (list == null || list.Status != "success" || list.Items.Count == 0)
                    break;

                if (page == 1 && list.Paginate != null)
                {
                    lastPage = maxPagesCap.HasValue
                        ? Math.Min(list.Paginate.TotalPage, maxPagesCap.Value)
                        : list.Paginate.TotalPage;
                }

                _log.LogInformation("Crawl [{Label}] trang {Page}/{Last} ({Count} phim)", logLabel, page, lastPage, list.Items.Count);
                Console.WriteLine($"  [{logLabel}] trang {page}/{lastPage}, {list.Items.Count} phim");

                var (n, u, e) = await ProcessSummarySlugsAsync(list.Items, skipIfExists: true, ct);
                newCount += n;
                updCount += u;
                epCount += e;

                page++;
                await Task.Delay(450, ct);
            }

            return CrawlResult.Ok(newMovies: newCount, updated: updCount, episodes: epCount,
                msg: $"{logLabel}: {newCount} moi, {updCount} cap nhat, {epCount} tap");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(ex, "CrawlPagedFilmListAsync {Label}", logLabel);
            return CrawlResult.Fail(ex.Message);
        }
    }

    public async Task<CrawlResult> CrawlListPageAsync(string url, CancellationToken ct = default)
    {
        try
        {
            _log.LogInformation("Crawling list HTML: {Url}", url);
            var html = await GetStringOrNullAsync(url, ct);
            if (html == null)
                return CrawlResult.Fail($"HTTP loi khi tai: {url}");

            var slugs = ExtractMovieSlugs(html);
            if (slugs.Count == 0)
                return CrawlResult.Fail($"Khong tim thay slug phim tren trang: {url}");

            var summaries = slugs.Select(s => new NguonCFilmSummaryDto { Slug = s, Name = s }).ToList();
            var (n, u, e) = await ProcessSummarySlugsAsync(summaries, skipIfExists: true, ct);
            return CrawlResult.Ok(newMovies: n, updated: u, episodes: e,
                msg: $"{n} moi, {u} cap nhat, {e} tap");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(ex, "CrawlListPageAsync {Url}", url);
            return CrawlResult.Fail(ex.Message);
        }
    }

    public async Task<CrawlResult> CrawlMovieDetailAsync(string slug, CancellationToken ct = default)
    {
        if (!IsValidMovieSlug(slug))
            return CrawlResult.Fail($"Slug khong hop le (placeholder hoac rong): {slug}");

        using IServiceScope scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var url = NguonCApiPaths.BuildFilmDetailUrl(slug);
            var json = await GetStringOrNullAsync(url, ct);
            if (json == null)
                return CrawlResult.Fail($"Phim khong ton tai hoac API tra loi: {slug}");

            var dto = JsonSerializer.Deserialize<NguonCResponseDto>(json, DetailJsonOptions);
            if (dto == null || dto.Status != "success")
                return CrawlResult.Fail($"Movie not found or API error: {slug}");

            var movie = dto.Movie;
            int year = ExtractYear(movie);
            int newCount = 0, updCount = 0, epCount = 0;

            var existing = await ctx.Movies
                .Include(m => m.Episodes)
                .Include(m => m.MovieCategories)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.ExternalId == slug && m.Source == "nguonc", ct);

            if (existing == null)
            {
                existing = MapToMovie(movie, slug, year);
                ctx.Movies.Add(existing);
                newCount++;
            }
            else
            {
                UpdateMovie(existing, movie, year);
                updCount++;
            }

            var categories = ExtractCategories(movie);
            var existingCatIds = existing.MovieCategories
                .Select(mc => mc.CategoryId)
                .ToHashSet();

            foreach (var cat in categories)
            {
                if (string.IsNullOrWhiteSpace(cat.Slug)) continue;

                var existingCat = await ctx.Categories.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Slug == cat.Slug, ct);
                if (existingCat == null)
                {
                    existingCat = new Category { Name = cat.Name, Slug = cat.Slug, IsActive = true };
                    ctx.Categories.Add(existingCat);
                    await ctx.SaveChangesAsync(ct);
                }

                if (!existingCatIds.Contains(existingCat.Id))
                {
                    existingCatIds.Add(existingCat.Id);
                    existing.MovieCategories.Add(new MovieCategory
                    {
                        Movie = existing,
                        CategoryId = existingCat.Id
                    });
                }
            }

            if (movie.Episodes.Count > 0)
            {
                var firstServer = movie.Episodes[0];
                int epNum = 1;

                foreach (var ep in firstServer.Items)
                {
                    if (string.IsNullOrWhiteSpace(ep.M3u8) && string.IsNullOrWhiteSpace(ep.Embed))
                        continue;

                    var existingEp = existing.Episodes.FirstOrDefault(e =>
                        e.EpisodeNumber == epNum && e.Server == firstServer.ServerName);

                    if (existingEp == null)
                    {
                        existing.Episodes.Add(new Episode
                        {
                            Movie = existing,
                            Name = $"Tap {ep.Name}",
                            EpisodeNumber = epNum,
                            EmbedLink = ep.Embed,
                            FileUrl = ep.M3u8,
                            Server = firstServer.ServerName,
                            Status = "active"
                        });
                        epCount++;
                    }
                }

                // Cap nhat so tap hien tai cho phim (neu co tap moi)
                if (epCount > 0)
                {
                    var currentEpCount = existing.Episodes.Count(e => e.Status == "active");
                    existing.UpdatedAt = DateTime.UtcNow;

                    // Neu so tap hien tai < so tap moi nhat tu API -> update
                    var maxEpNum = firstServer.Items
                        .Where(ep => !string.IsNullOrWhiteSpace(ep.M3u8) || !string.IsNullOrWhiteSpace(ep.Embed))
                        .Select((ep, idx) => idx + 1)
                        .DefaultIfEmpty(0)
                        .Max();

                    if (maxEpNum > currentEpCount)
                    {
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            await ctx.SaveChangesAsync(ct);
            return CrawlResult.Ok(newMovies: newCount, updated: updCount, episodes: epCount,
                msg: $"{movie.Name} (nam {year}): {newCount} moi, {updCount} cap nhat, {epCount} tap");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error crawling movie {Slug}", slug);
            return CrawlResult.Fail(ex.Message);
        }
    }

    private async Task<(int New, int Updated, int Episodes)> ProcessSummarySlugsAsync(
        IReadOnlyList<NguonCFilmSummaryDto> items,
        bool skipIfExists,
        CancellationToken ct)
    {
        int newCount = 0, updCount = 0, epCount = 0;
        int total = items.Count;

        await Parallel.ForEachAsync(items, new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = ct
        }, async (item, innerCt) =>
        {
            var slug = item.Slug?.Trim() ?? string.Empty;
            if (!IsValidMovieSlug(slug))
            {
                _log.LogWarning("Bo qua slug khong hop le: {Slug}", slug);
                return;
            }

            try
            {
                if (skipIfExists)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var exists = await ctx.Movies.AsNoTracking()
                        .AnyAsync(m => m.ExternalId == slug && m.Source == "nguonc", innerCt);
                    if (exists)
                    {
                        await Task.Delay(80, innerCt);
                        return;
                    }
                }

                var detail = await CrawlMovieDetailAsync(slug, innerCt);
                if (detail.Success)
                {
                    Interlocked.Add(ref newCount, detail.NewMovies);
                    Interlocked.Add(ref updCount, detail.UpdatedMovies);
                    Interlocked.Add(ref epCount, detail.EpisodesProcessed);
                }
                else
                {
                    var err = detail.Errors.FirstOrDefault() ?? detail.Message ?? "unknown";
                    Console.WriteLine($"  [PARALLEL] LOI {slug}: {err}");
                }

                await Task.Delay(400, innerCt);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ProcessSummarySlugsAsync loi voi slug: {Slug}", slug);
            }
        });

        return (newCount, updCount, epCount);
    }

    private async Task<NguonCFilmsListResponse?> FetchFilmsListAsync(string url, CancellationToken ct)
    {
        var json = await GetStringOrNullAsync(url, ct);
        if (json == null) return null;
        try
        {
            return JsonSerializer.Deserialize<NguonCFilmsListResponse>(json, ListJsonOptions);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Deserialize films list failed: {Url}", url);
            return null;
        }
    }

    private async Task<string?> GetStringOrNullAsync(string url, CancellationToken ct)
    {
        using var res = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!res.IsSuccessStatusCode)
        {
            _log.LogWarning("HTTP {Code} GET {Url}", res.StatusCode, url);
            return null;
        }

        return await res.Content.ReadAsStringAsync(ct);
    }

    private static bool IsValidMovieSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        if (slug.Length > 256) return false;
        if (slug.Contains('{') || slug.Contains('}')) return false;
        if (slug.Contains(' ', StringComparison.Ordinal)) return false;
        return true;
    }

    private static int ExtractYear(NguonCMovieDto movie)
    {
        if (movie.Category.TryGetValue("3", out var yearGroup))
        {
            var firstItem = yearGroup.List.FirstOrDefault();
            if (firstItem != null)
            {
                var name = firstItem.Name.Trim();
                if (int.TryParse(name, out int year))
                    return year;
            }
        }
        return 0;
    }

    private static List<CategoryRef> ExtractCategories(NguonCMovieDto movie)
    {
        var result = new List<CategoryRef>();

        if (movie.Category.TryGetValue("2", out var typeGroup))
        {
            foreach (var item in typeGroup.List)
                result.Add(new CategoryRef(item.Name, SlugHelper.ToSlug(item.Name)));
        }

        if (movie.Category.TryGetValue("4", out var countryGroup))
        {
            foreach (var item in countryGroup.List)
                result.Add(new CategoryRef(item.Name, SlugHelper.ToSlug(item.Name)));
        }

        return result;
    }

    private static List<string> ExtractMovieSlugs(string html)
    {
        var slugs = new List<string>();
        var pattern = @"href=""(/phim/[^""]+)""";
        var matches = Regex.Matches(html, pattern);
        foreach (Match match in matches)
        {
            var path = match.Groups[1].Value;
            var slug = path.Replace("/phim/", "", StringComparison.Ordinal).Trim();
            if (!string.IsNullOrWhiteSpace(slug) && !slugs.Contains(slug))
                slugs.Add(slug);
        }

        return slugs.Distinct().ToList();
    }

    private static bool IsCompletedStatus(string? currentEpisode)
    {
        if (string.IsNullOrWhiteSpace(currentEpisode)) return true;
        var s = currentEpisode.Trim();
        if (s.Equals("FULL", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Contains("tap cuoi", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Contains("Hoàn tất", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Contains("Hoan tat", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static Movie MapToMovie(NguonCMovieDto m, string slug, int year)
    {
        var duration = int.TryParse(Regex.Replace(m.Time ?? "", "[^0-9]", ""), out var d) ? d : (int?)null;
        var type = m.TotalEpisodes > 1 ? "series" : "single";
        var status = IsCompletedStatus(m.CurrentEpisode) ? "completed" : "ongoing";

        return new Movie
        {
            ExternalId = slug,
            Name = m.Name,
            Slug = slug,
            OriginName = m.OriginalName,
            Thumb = m.ThumbUrl,
            Poster = m.PosterUrl,
            Year = year > 0 ? year : null,
            Description = m.Description,
            Duration = duration,
            ImdbScore = null,
            ViewCount = 0,
            Status = status,
            Quality = m.Quality,
            Lang = m.Language,
            Type = type,
            Source = "nguonc",
            IsPublished = true
        };
    }

    private static void UpdateMovie(Movie movie, NguonCMovieDto m, int year)
    {
        movie.Name = m.Name;
        movie.OriginName = m.OriginalName;
        movie.Thumb = m.ThumbUrl;
        movie.Poster = m.PosterUrl;
        movie.Year = year > 0 ? year : movie.Year;
        movie.Description = m.Description;
        movie.Quality = m.Quality;
        movie.Lang = m.Language;
        movie.Status = IsCompletedStatus(m.CurrentEpisode) ? "completed" : "ongoing";
        movie.UpdatedAt = DateTime.UtcNow;
    }

    private record CategoryRef(string Name, string Slug);
}
