namespace SunPhim.Models;

public class Movie
{
    public int Id { get; set; }

    /// <summary>
    /// Tên phim đầy đủ
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn SEO: /phim-avengers-tap-1
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Tên gốc hoặc tên khác của phim
    /// </summary>
    public string? OriginName { get; set; }

    /// <summary>
    /// Hình thumbnail (ảnh nhỏ, dùng cho danh sách)
    /// </summary>
    public string? Thumb { get; set; }

    /// <summary>
    /// Poster (ảnh lớn, dùng cho trang chi tiết)
    /// </summary>
    public string? Poster { get; set; }

    /// <summary>
    /// Năm phát hành
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Mô tả nội dung phim
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Thời lượng phim (phút)
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Điểm IMDB (ví dụ: 8.5)
    /// </summary>
    public double? ImdbScore { get; set; }

    /// <summary>
    /// Số lượt xem ảo
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// Trạng thái phim: "ongoing", "completed", "trailer"
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Chất lượng video: "HD", "FullHD", "4K", "SD"
    /// </summary>
    public string? Quality { get; set; }

    /// <summary>
    /// Ngôn ngữ phim: "Vietsub", "Thuyết minh", "Lồng tiếng"
    /// </summary>
    public string? Lang { get; set; }

    /// <summary>
    /// Đường dẫn embed (iframe/video player bên thứ 3)
    /// </summary>
    public string? EmbedLink { get; set; }

    /// <summary>
    /// Đường dẫn tuyệt đối đến file/video gốc
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// Nguồn cào data: "ophim", "kkphim", "tmdb", "manual"
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// ID phim từ source gốc (để tránh trùng lặp khi cào lại)
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Ngày tạo bản ghi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ngày cập nhật cuối
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Hiển thị công khai hay không
    /// </summary>
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// Loại phim: "series" (phim bộ), "single" (phim lẻ), "tv"
    /// </summary>
    public string Type { get; set; } = "single";

    /// <summary>
    /// Các tập phim thuộc phim này
    /// </summary>
    public ICollection<Episode> Episodes { get; set; } = new List<Episode>();

    /// <summary>
    /// Các thể loại chứa phim này (quan hệ N-N)
    /// </summary>
    public ICollection<MovieCategory> MovieCategories { get; set; } = new List<MovieCategory>();
}
