namespace SunPhim.Models;

public class WatchHistory
{
    public int Id { get; set; }

    /// <summary>
    /// ID phim đã xem
    /// </summary>
    public int MovieId { get; set; }

    /// <summary>
    /// Phim đã xem
    /// </summary>
    public Movie? Movie { get; set; }

    /// <summary>
    /// ID tập đã xem (null nếu là phim lẻ)
    /// </summary>
    public int? EpisodeId { get; set; }

    /// <summary>
    /// Tập đã xem
    /// </summary>
    public Episode? Episode { get; set; }

    /// <summary>
    /// Tài khoản người dùng (nếu có, null nếu anonymous)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Địa chỉ IP (dùng cho anonymous)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent trình duyệt
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Thời điểm xem
    /// </summary>
    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời điểm xem dừng lại (giây)
    /// </summary>
    public int? StoppedAt { get; set; }
}
