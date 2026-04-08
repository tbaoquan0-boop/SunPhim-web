using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunPhim.Data;

namespace SunPhim.Controllers;

[Route("api/stream")]
[ApiController]
public class StreamingController : ControllerBase
{
    private readonly HttpClient _http;
    private readonly AppDbContext _ctx;
    private readonly ILogger<StreamingController> _log;
    private readonly Services.Streaming.IAntiHotlinkService _hotlink;
    private readonly Services.Streaming.IGDriveService _gdrive;
    private readonly IConfiguration _config;

    private static readonly Regex M3u8DomainRegex = new(@"(https?://[^/]+)", RegexOptions.Compiled);

    public StreamingController(
        HttpClient http,
        AppDbContext ctx,
        ILogger<StreamingController> log,
        Services.Streaming.IAntiHotlinkService hotlink,
        Services.Streaming.IGDriveService gdrive,
        IConfiguration config)
    {
        _http = http;
        _ctx = ctx;
        _log = log;
        _hotlink = hotlink;
        _gdrive = gdrive;
        _config = config;
    }

    [HttpGet("{videoId}")]
    public async Task<IActionResult> StreamVideo(string videoId, [FromQuery] string? token)
    {
        if (!_hotlink.ValidateToken(token ?? "", videoId))
            return Unauthorized(new { error = "Invalid or expired token" });

        var episode = await _ctx.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.MovieId.ToString() == videoId || e.Id.ToString() == videoId);

        if (episode?.FileUrl == null)
            return NotFound(new { error = "Video not found" });

        return Redirect(episode.FileUrl);
    }

    [HttpGet("proxy")]
    public async Task<IActionResult> ProxyM3u8([FromQuery] string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "URL is required" });

        try
        {
            var res = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!res.IsSuccessStatusCode)
                return StatusCode((int)res.StatusCode, new { error = "Failed to fetch stream" });

            var contentType = res.Content.Headers.ContentType?.MediaType ?? "application/vnd.apple.mpegurl";
            var content = await res.Content.ReadAsStringAsync(ct);

            var proxyBase = _config["Streaming:ProxyBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            content = RewriteM3u8Urls(content, url, proxyBase);

            return Content(content, contentType, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error proxying m3u8 from {Url}", url);
            return StatusCode(500, new { error = "Proxy failed" });
        }
    }

    [HttpGet("player/{episodeId:int}")]
    public async Task<IActionResult> GetPlayerUrl(int episodeId, [FromQuery] bool proxy = false)
    {
        var episode = await _ctx.Episodes
            .AsNoTracking()
            .Include(e => e.Movie)
            .FirstOrDefaultAsync(e => e.Id == episodeId);

        if (episode == null)
            return NotFound(new { error = "Episode not found" });

        var embedUrl = episode.EmbedLink;
        if (string.IsNullOrWhiteSpace(embedUrl))
        {
            if (!string.IsNullOrWhiteSpace(episode.FileUrl))
            {
                if (episode.FileUrl.Contains("drive.google.com"))
                {
                    var directUrl = await _gdrive.GetDirectStreamUrlAsync(episode.FileUrl);
                    embedUrl = directUrl ?? episode.FileUrl;
                }
                else if (proxy)
                {
                    embedUrl = $"/api/stream/proxy?url={Uri.EscapeDataString(episode.FileUrl)}";
                }
                else
                {
                    embedUrl = episode.FileUrl;
                }
            }
        }

        if (episode.Movie != null)
        {
            episode.Movie.ViewCount++;
            await _ctx.SaveChangesAsync();
        }

        return Ok(new
        {
            episodeId = episode.Id,
            playerUrl = embedUrl,
            fileUrl = episode.FileUrl,
            server = episode.Server,
            movieName = episode.Movie?.Name,
            protectedUrl = _hotlink.BuildProtectedUrl(episode.Id.ToString())
        });
    }

    [HttpGet("gdrive/resolve")]
    public async Task<IActionResult> ResolveGDrive([FromQuery] string url)
    {
        var streamUrl = await _gdrive.GetDirectStreamUrlAsync(url);
        if (streamUrl == null)
            return NotFound(new { error = "Could not resolve Google Drive URL" });
        return Ok(new { streamUrl });
    }

    [HttpPost("report")]
    public async Task<IActionResult> ReportBroken([FromBody] ReportRequest req)
    {
        if (req.EpisodeId <= 0)
            return BadRequest(new { error = "Invalid episode ID" });

        var episode = await _ctx.Episodes.FindAsync(req.EpisodeId);
        if (episode == null)
            return NotFound();

        episode.Status = "broken";
        await _ctx.SaveChangesAsync();

        _log.LogWarning("Broken stream reported for episode {EpisodeId}", req.EpisodeId);
        return Ok(new { message = "Reported successfully" });
    }

    private static string RewriteM3u8Urls(string content, string originalUrl, string proxyBase)
    {
        var baseDomain = M3u8DomainRegex.Match(originalUrl).Groups[1].Value;

        content = M3u8DomainRegex.Replace(content, match =>
        {
            var original = match.Groups[1].Value;
            return $"{proxyBase}/api/stream/proxy?url={Uri.EscapeDataString(original)}";
        });

        return content;
    }
}

public class ReportRequest
{
    public int EpisodeId { get; set; }
    public string? Reason { get; set; }
}