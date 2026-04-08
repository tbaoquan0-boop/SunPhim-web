using System.Net.Http.Headers;
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
        "img.ophim.live",
        "image.tmdb.org",
        "m.media-imdb.com",
        "upload.wikimedia.org",
        "vignette.wikia.nocookie.net"
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

    [HttpGet("proxy")]
    public async Task<IActionResult> ProxyImage([FromQuery] string url, [FromQuery] int width = 0)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "URL is required" });

        // Validate host
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsAllowedHost(uri.Host))
        {
            _log.LogWarning("Blocked proxy request for host: {Host}", uri?.Host);
            return BadRequest(new { error = "Host not allowed" });
        }

        var cacheKey = $"img:{width}:{url.GetHashCode()}";

        var cached = await _cache.GetAsync<byte[]>(cacheKey);
        if (cached != null)
        {
            Response.Headers.CacheControl = $"public, max-age={CacheDuration.TotalSeconds}";
            return File(cached, GetContentType(url));
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; SunPhim/1.0)");
            req.Headers.Referrer = new Uri("https://ophim1.com/");

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

        if (movie?.Thumb == null)
            return NotFound(new { error = "Thumbnail not found" });

        if (movie.Thumb.StartsWith("http"))
            return Redirect(movie.Thumb);

        return Redirect($"/api/image/proxy?url={Uri.EscapeDataString(movie.Thumb)}&width={size}");
    }

    [HttpGet("poster/{movieId:int}")]
    public async Task<IActionResult> GetMoviePoster(int movieId)
    {
        var movie = await _ctx.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == movieId);

        if (movie?.Poster == null)
            return NotFound(new { error = "Poster not found" });

        if (movie.Poster.StartsWith("http"))
            return Redirect(movie.Poster);

        return Redirect($"/api/image/proxy?url={Uri.EscapeDataString(movie.Poster)}");
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