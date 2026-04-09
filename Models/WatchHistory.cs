namespace SunPhim.Models;

public class WatchHistory
{
    public int Id { get; set; }

    public int MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int? EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

    public int? StoppedAt { get; set; }
}
