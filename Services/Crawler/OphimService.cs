using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SunPhim.Data;
using SunPhim.Helpers;
using SunPhim.Models;
using SunPhim.Models.Crawler;

namespace SunPhim.Services.Crawler;

public class OphimService : IOphimService
{
    private readonly HttpClient _http;
    private readonly AppDbContext _ctx;
    private readonly ILogger<OphimService> _log;
    private const string BaseUrl = "https://ophim1.com";
    private const string ImgHost = "https://img.ophim.live/uploads/movies/";

    public OphimService(HttpClient http, AppDbContext ctx, ILogger<OphimService> log)
    {
        _http = http;
        _ctx = ctx;
        _log = log;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; SunPhimBot/1.0)");
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<CrawlResult> CrawlNewUpdatesAsync(int page = 1, CancellationToken ct = default)
    {
        _log.LogInformation("Crawling new updates page {Page}", page);
        try
        {
            var listUrl = $"{BaseUrl}/danh-sach/phim-moi-cap-nhat?page={page}";
            var listRes = await FetchAsync<OphimListResponse>(listUrl, ct);
            if (listRes == null || !listRes.Status || listRes.Items.Count == 0)
                return CrawlResult.Fail($"Failed to fetch list page {page}");

            int newCount = 0, updCount = 0, epCount = 0;

            foreach (var item in listRes.Items)
            {
                ct.ThrowIfCancellationRequested();

                bool exists = await _ctx.Movies.AnyAsync(m => m.ExternalId == item.MongoId, ct);
                if (exists)
                {
                    newCount++;
                    continue;
                }

                var detail = await CrawlMovieDetailAsync(item.Slug, ct);
                if (detail.Success)
                {
                    newCount += detail.NewMovies;
                    updCount += detail.UpdatedMovies;
                    epCount += detail.EpisodesProcessed;
                }
            }

            _log.LogInformation("Page {Page}: {New} new, {Upd} updated, {Ep} episodes", page, newCount, updCount, epCount);
            return CrawlResult.Ok(newMovies: newCount, updated: updCount, episodes: epCount,
                msg: $"Trang {page}: {newCount} mới, {updCount} cập nhật, {epCount} tập");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error crawling new updates page {Page}", page);
            return CrawlResult.Fail(ex.Message);
        }
    }

    public async Task<CrawlResult> CrawlMovieListAsync(int page = 1, CancellationToken ct = default)
        => await CrawlNewUpdatesAsync(page, ct);

    public async Task<CrawlResult> CrawlMovieDetailAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/phim/{slug}";
            var detail = await FetchAsync<OphimMovieDetail>(url, ct);
            if (detail == null || !detail.Status)
                return CrawlResult.Fail($"Movie not found: {slug}");

            return await UpsertMovieAsync(detail, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error crawling movie {Slug}", slug);
            return CrawlResult.Fail(ex.Message);
        }
    }

    public async Task<int> SyncAllPagesAsync(int maxPages = 10, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/danh-sach/phim-moi-cap-nhat?page=1";
        var first = await FetchAsync<OphimListResponse>(url, ct);
        if (first == null) return 0;

        int totalPages = Math.Min(first.Pagination.TotalPages, maxPages);
        int synced = 0;

        for (int page = 1; page <= totalPages; page++)
        {
            ct.ThrowIfCancellationRequested();
            var result = await CrawlNewUpdatesAsync(page, ct);
            if (result.Success) synced++;
        }

        return synced;
    }

    private async Task<CrawlResult> UpsertMovieAsync(OphimMovieDetail detail, CancellationToken ct)
    {
        var ophim = detail.Movie;
        var categories = ophim.Category.Concat(ophim.Country).ToList();

        int newCount = 0, updCount = 0, epCount = 0;

        var movie = await _ctx.Movies
            .Include(m => m.Episodes)
            .Include(m => m.MovieCategories)
            .FirstOrDefaultAsync(m => m.ExternalId == ophim.MongoId, ct);

        if (movie == null)
        {
            movie = MapToMovie(ophim);
            _ctx.Movies.Add(movie);
            newCount++;
        }
        else
        {
            UpdateMovie(movie, ophim);
            updCount++;
        }

        foreach (var cat in categories)
        {
            if (string.IsNullOrWhiteSpace(cat.Slug)) continue;
            var existingCat = await _ctx.Categories.FirstOrDefaultAsync(c => c.Slug == cat.Slug, ct);
            if (existingCat == null)
            {
                existingCat = new Category { Name = cat.Name, Slug = cat.Slug, IsActive = true };
                _ctx.Categories.Add(existingCat);
                await _ctx.SaveChangesAsync(ct);
            }

            if (!movie.MovieCategories.Any(mc => mc.CategoryId == existingCat.Id))
            {
                movie.MovieCategories.Add(new MovieCategory { Movie = movie, Category = existingCat });
            }
        }

        if (detail.Episodes.Count > 0)
        {
            foreach (var server in detail.Episodes)
            {
                int epNum = 1;
                foreach (var ep in server.ServerData)
                {
                    if (string.IsNullOrWhiteSpace(ep.LinkM3u8) && string.IsNullOrWhiteSpace(ep.LinkEmbed))
                        continue;

                    var existing = movie.Episodes.FirstOrDefault(e =>
                        e.EpisodeNumber == epNum && e.Server == server.ServerName);

                    if (existing == null)
                    {
                        movie.Episodes.Add(new Episode
                        {
                            Movie = movie,
                            Name = $"Tập {ep.Name}",
                            EpisodeNumber = epNum,
                            EmbedLink = ep.LinkEmbed,
                            FileUrl = ep.LinkM3u8,
                            Server = server.ServerName,
                            Status = "active"
                        });
                        epCount++;
                    }
                    epNum++;
                }
            }
        }

        await _ctx.SaveChangesAsync(ct);
        return CrawlResult.Ok(newMovies: newCount, updated: updCount, episodes: epCount);
    }

    private static Movie MapToMovie(OphimMovie o)
    {
        var duration = int.TryParse(Regex.Replace(o.Time ?? "", "[^0-9]", ""), out var d) ? d : (int?)null;
        var type = o.Type == "series" ? "series" : "single";
        var status = o.Status switch
        {
            "completed" => "completed",
            "ongoing" => "ongoing",
            _ => "completed"
        };

        return new Movie
        {
            ExternalId = o.MongoId,
            Name = o.Name,
            Slug = SlugHelper.ToSlug(o.Name),
            OriginName = o.OriginName,
            Thumb = NormalizeImageUrl(o.ThumbUrl),
            Poster = NormalizeImageUrl(o.PosterUrl),
            Year = o.Year > 0 ? o.Year : null,
            Description = StripHtml(o.Content),
            Duration = duration,
            ImdbScore = o.Imdb.VoteAverage > 0 ? o.Imdb.VoteAverage : null,
            ViewCount = o.View,
            Status = status,
            Quality = o.Quality,
            Lang = o.Lang,
            Type = type,
            Source = "ophim",
            IsPublished = true
        };
    }

    private static void UpdateMovie(Movie movie, OphimMovie o)
    {
        movie.Name = o.Name;
        movie.OriginName = o.OriginName;
        movie.Thumb = NormalizeImageUrl(o.ThumbUrl);
        movie.Poster = NormalizeImageUrl(o.PosterUrl);
        movie.Year = o.Year > 0 ? o.Year : movie.Year;
        movie.Description = StripHtml(o.Content);
        movie.ImdbScore = o.Imdb.VoteAverage > 0 ? o.Imdb.VoteAverage : movie.ImdbScore;
        movie.ViewCount = o.View;
        movie.Status = o.Status == "ongoing" ? "ongoing" : "completed";
        movie.Quality = o.Quality;
        movie.Lang = o.Lang;
        movie.UpdatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (url.StartsWith("http")) return url;
        if (url.StartsWith("/")) return $"{ImgHost.TrimEnd('/')}{url}";
        return $"{ImgHost}{url}";
    }

    private static string StripHtml(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        return Regex.Replace(s, "<.*?>", string.Empty).Trim();
    }

    private async Task<T?> FetchAsync<T>(string url, CancellationToken ct) where T : class
    {
        var res = await _http.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode)
        {
            _log.LogWarning("HTTP {Status} for {Url}", res.StatusCode, url);
            return null;
        }
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
    }
}
