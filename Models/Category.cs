namespace SunPhim.Models;

public class Category
{
    public int Id { get; set; }

    /// <summary>
    /// Tên thể loại: "Hành Động", "Hài Hước", "Tình Cảm"...
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn SEO: /the-loai/hanh-dong
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả thể loại
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Hiển thị trên trang chủ không
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Ngày tạo
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Các phim thuộc thể loại này (quan hệ N-N)
    /// </summary>
    public ICollection<MovieCategory> MovieCategories { get; set; } = new List<MovieCategory>();
}
