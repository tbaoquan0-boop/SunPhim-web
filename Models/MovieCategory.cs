namespace SunPhim.Models;

/// <summary>
/// Bảng trung gian cho quan hệ N-N giữa Movie và Category
/// </summary>
public class MovieCategory
{
    public int MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}
