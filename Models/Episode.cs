namespace SunPhim.Models;

public class Episode
{
    public int Id { get; set; }

    /// <summary>
    /// Tên tập (ví dụ: "Tập 1", "Tập 2", "Episode 1")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Số tập (dùng để sắp xếp)
    /// </summary>
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// ID phim cha
    /// </summary>
    public int MovieId { get; set; }

    /// <summary>
    /// Phim cha
    /// </summary>
    public Movie? Movie { get; set; }

    /// <summary>
    /// Đường dẫn embed / iframe nhúng player bên thứ 3
    /// </summary>
    public string? EmbedLink { get; set; }

    /// <summary>
    /// Đường dẫn tuyệt đối đến file m3u8 / mp4
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// Server nguồn: "oshare", "hydrahd", "googledrive", "streamango"...
    /// </summary>
    public string? Server { get; set; }

    /// <summary>
    /// Trạng thái: "active", "broken", "waiting"
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Ngày tạo
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày cập nhật
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
