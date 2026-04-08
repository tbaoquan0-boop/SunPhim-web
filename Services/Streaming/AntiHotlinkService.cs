using System.Security.Cryptography;
using System.Text;

namespace SunPhim.Services.Streaming;

public interface IAntiHotlinkService
{
    string GenerateToken(string videoId, int expiresInMinutes = 60);
    bool ValidateToken(string token, string videoId);
    string BuildProtectedUrl(string videoId, int expiresInMinutes = 60);
}

public class AntiHotlinkService : IAntiHotlinkService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AntiHotlinkService> _log;

    public AntiHotlinkService(IConfiguration config, ILogger<AntiHotlinkService> log)
    {
        _config = config;
        _log = log;
    }

    private string SecretKey => _config["Streaming:HotlinkSecret"] ?? "sunphim-default-secret-change-me";

    public string GenerateToken(string videoId, int expiresInMinutes = 60)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes).ToUnixTimeSeconds();
        var payload = $"{videoId}:{expires}";
        var signature = ComputeHmac(payload);
        return $"{payload}:{signature}";
    }

    public bool ValidateToken(string token, string videoId)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parts = token.Split(':');
        if (parts.Length != 3) return false;

        var payload = $"{parts[0]}:{parts[1]}";
        var expectedSig = ComputeHmac(payload);

        if (!string.Equals(parts[2], expectedSig, StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("Invalid hotlink signature for video {VideoId}", videoId);
            return false;
        }

        if (!long.TryParse(parts[1], out var expiresUnix))
        {
            return false;
        }

        var expires = DateTimeOffset.FromUnixTimeSeconds(expiresUnix);
        if (expires < DateTimeOffset.UtcNow)
        {
            _log.LogWarning("Expired hotlink token for video {VideoId}", videoId);
            return false;
        }

        if (!string.Equals(parts[0], videoId, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    public string BuildProtectedUrl(string videoId, int expiresInMinutes = 60)
    {
        var token = GenerateToken(videoId, expiresInMinutes);
        return $"/api/stream/{videoId}?token={Uri.EscapeDataString(token)}";
    }

    private string ComputeHmac(string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(SecretKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}