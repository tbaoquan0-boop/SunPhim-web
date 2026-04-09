namespace SunPhim.Models.Crawler;

/// <summary>
/// JSON: GET /api/films/phim-moi-cap-nhat, /api/films/danh-sach/...
/// </summary>
public class NguonCFilmsListResponse
{
    public string Status { get; set; } = string.Empty;
    public NguonCFilmsPaginateDto? Paginate { get; set; }
    public List<NguonCFilmSummaryDto> Items { get; set; } = new();
}

public class NguonCFilmsPaginateDto
{
    public int CurrentPage { get; set; }
    public int TotalPage { get; set; }
    public int TotalItems { get; set; }
    public int ItemsPerPage { get; set; }
}

/// <summary>
/// Phan tu trong danh sach API (truong hop giong tom tat phim).
/// </summary>
public class NguonCFilmSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string ThumbUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalEpisodes { get; set; }
    public string CurrentEpisode { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}
