using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunPhim.Data;

namespace SunPhim.Controllers;

[Route("api/image")]
[ApiController]
public class ImageController : ControllerBase
{
    private readonly HttpClient _http;
    private readonly AppDbContext _ctx;
    private readonly Services.Cache.ICacheService _cache;
    private readonly ILogger<ImageController> _log;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(1);

    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "image.tmdb.org",
        "m.media-imdb.com",
        "upload.wikimedia.org",
        "vignette.wikia.nocookie.net",
        "phim.nguonc.com"
    };

    public ImageController(
        HttpClient http,
        AppDbContext ctx,
        Services.Cache.ICacheService cache,
        ILogger<ImageController> log)
    {
        _http = http;
        _ctx = ctx;
        _cache = cache;
        _log = log;
    }

    /// <param name="cid">Dinh danh phim (id hoac slug) — tach cache theo phim khi trung URL.</param>
    [HttpGet("proxy")]
    public async Task<IActionResult> ProxyImage([FromQuery] string url, [FromQuery] int width = 0, [FromQuery] string? cid = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "URL is required" });

        // Validate host
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsAllowedHost(uri.Host))
        {
            _log.LogWarning("Blocked proxy request for host: {Host}", uri?.Host);
            return BadRequest(new { error = "Host not allowed" });
        }

        // KHONG dung url.GetHashCode() — hash 32-bit de trung → nhieu URL khac nhau lay cung 1 anh cache.
        var urlHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        var cacheKey = string.IsNullOrWhiteSpace(cid)
            ? $"img:{width}:{urlHash}"
            : $"img:{width}:{urlHash}:cid:{cid.Trim()}";

        var cached = await _cache.GetAsync<byte[]>(cacheKey);
        if (cached != null)
        {
            Response.Headers.CacheControl = $"public, max-age={CacheDuration.TotalSeconds}";
            return File(cached, GetContentType(url));
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            // NguonC va mot so host chan hotlink: can Referer dung domain nguon.
            req.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            if (uri.Host.Contains("nguonc", StringComparison.OrdinalIgnoreCase))
            {
                req.Headers.Referrer = new Uri("https://phim.nguonc.com/");
                req.Headers.TryAddWithoutValidation("Origin", "https://phim.nguonc.com");
            }
            req.Headers.TryAddWithoutValidation("Accept", "image/avif,image/webp,image/apng,image/*,*/*;q=0.8");

            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseContentRead);

            if (!res.IsSuccessStatusCode)
                return StatusCode((int)res.StatusCode, "Failed to fetch image");

            var contentType = res.Content.Headers.ContentType?.MediaType ?? GetContentType(url);
            var bytes = await res.Content.ReadAsByteArrayAsync();

            await _cache.SetAsync(cacheKey, bytes, CacheDuration);

            Response.Headers.CacheControl = $"public, max-age={CacheDuration.TotalSeconds}";
            return File(bytes, contentType);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Image proxy failed for {Url}", url);
            return StatusCode(500, "Image proxy failed");
        }
    }

    [HttpGet("thumb/{movieId:int}")]
    public async Task<IActionResult> GetMovieThumb(int movieId, [FromQuery] int size = 0)
    {
        var movie = await _ctx.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == movieId);

        var img = FirstHttpImageUrl(movie?.Thumb, movie?.Poster);
        if (img == null)
            return NotFound(new { error = "Thumbnail not found" });

        if (Uri.TryCreate(img, UriKind.Absolute, out var tu) &&
            tu.Host.Contains("nguonc", StringComparison.OrdinalIgnoreCase))
            return Redirect($"/api/image/proxy?url={Uri.EscapeDataString(img)}&width={size}&cid={movieId}");

        return Redirect(img);
    }

    [HttpGet("poster/{movieId:int}")]
    public async Task<IActionResult> GetMoviePoster(int movieId, [FromQuery] int width = 0)
    {
        var movie = await _ctx.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == movieId);

        var img = FirstHttpImageUrl(movie?.Poster, movie?.Thumb);
        if (img == null)
            return NotFound(new { error = "Poster not found" });

        if (Uri.TryCreate(img, UriKind.Absolute, out var pu) &&
            pu.Host.Contains("nguonc", StringComparison.OrdinalIgnoreCase))
            return Redirect($"/api/image/proxy?url={Uri.EscapeDataString(img)}&width={width}&cid={movieId}");

        return Redirect(img);
    }

    private static string? FirstHttpImageUrl(params string?[] candidates)
    {
        foreach (var s in candidates)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            var t = s.Trim();
            if (t.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return t;
        }

        return null;
    }

    private static bool IsAllowedHost(string host)
    {
        foreach (var allowed in AllowedHosts)
        {
            if (host.EndsWith(allowed, StringComparison.OrdinalIgnoreCase) ||
                host.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string GetContentType(string url)
    {
        var ext = Path.GetExtension(url).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}