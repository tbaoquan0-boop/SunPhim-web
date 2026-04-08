using System.Text.Json;
using System.Text.RegularExpressions;

namespace SunPhim.Services.Streaming;

public interface IGDriveService
{
    Task<string?> GetDirectStreamUrlAsync(string gdriveUrl, CancellationToken ct = default);
    string? ExtractFileId(string url);
}

public class GDriveService : IGDriveService
{
    private readonly HttpClient _http;
    private readonly ILogger<GDriveService> _log;
    private const string GDRIVE_API = "https://drive.google.com";

    public GDriveService(HttpClient http, ILogger<GDriveService> log)
    {
        _http = http;
        _log = log;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; SunPhim/1.0)");
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public string? ExtractFileId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var match = Regex.Match(url, @"/file/d/([a-zA-Z0-9_-]{10,})");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(url, @"id=([a-zA-Z0-9_-]{10,})");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(url, @"([a-zA-Z0-9_-]{10,})/", RegexOptions.RightToLeft);
        if (match.Success) return match.Groups[1].Value;

        return null;
    }

    public async Task<string?> GetDirectStreamUrlAsync(string gdriveUrl, CancellationToken ct = default)
    {
        var fileId = ExtractFileId(gdriveUrl);
        if (string.IsNullOrWhiteSpace(fileId))
        {
            _log.LogWarning("Invalid Google Drive URL: {Url}", gdriveUrl);
            return null;
        }

        try
        {
            // Method 1: Use /uc endpoint (deprecated but still works)
            var ucUrl = $"{GDRIVE_API}/uc?id={fileId}&export=download";
            var res = await _http.GetAsync(ucUrl, HttpCompletionOption.ResponseHeadersRead, ct);

            if (res.IsSuccessStatusCode)
            {
                var disposition = res.Content.Headers.ContentDisposition?.FileName;
                if (!string.IsNullOrEmpty(disposition) && !disposition.Contains(".googleusercontent"))
                {
                    var location = res.Headers.Location?.ToString();
                    if (!string.IsNullOrEmpty(location) && location.StartsWith("http"))
                        return location;
                }
            }

            // Method 2: Use export/embed links
            var exportUrl = $"{GDRIVE_API}/embed/{fileId}";
            var embedRes = await _http.GetAsync(exportUrl, ct);
            if (embedRes.IsSuccessStatusCode)
            {
                var html = await embedRes.Content.ReadAsStringAsync(ct);
                var m3u8Match = Regex.Match(html, @"(https://[^""'\s]+\.m3u8[^""'\s]*)", RegexOptions.IgnoreCase);
                if (m3u8Match.Success)
                    return m3u8Match.Value;

                var mp4Match = Regex.Match(html, @"(https://[^""'\s]+\.mp4[^""'\s]*)", RegexOptions.IgnoreCase);
                if (mp4Match.Success)
                    return mp4Match.Value;
            }

            // Method 3: video.google.com direct streaming
            var videoUrl = $"https://video.google.com/get_player?docid={fileId}&formats=mp4&hd=1";
            var videoRes = await _http.GetAsync(videoUrl, ct);
            if (videoRes.IsSuccessStatusCode)
            {
                var content = await videoRes.Content.ReadAsStringAsync(ct);
                var streamMatch = Regex.Match(content, @"stream_url['""]?\s*:\s*['""]([^'""]+)['""]");
                if (streamMatch.Success)
                    return streamMatch.Groups[1].Value;
            }

            // Return a known working embed format
            return $"https://drive.google.com/uc?export=download&id={fileId}";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error extracting GDrive stream URL for {FileId}", fileId);
            return $"https://drive.google.com/uc?export=download&id={fileId}";
        }
    }
}