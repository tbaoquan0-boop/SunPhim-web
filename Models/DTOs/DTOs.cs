namespace SunPhim.Models.DTOs;

public class MovieListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? OriginName { get; set; }
    public string? Thumb { get; set; }
    public string? Poster { get; set; }
    public int? Year { get; set; }
    public string Type { get; set; } = "single";
    public string? Quality { get; set; }
    public string? Lang { get; set; }
    public double? ImdbScore { get; set; }
    public int ViewCount { get; set; }
    public string? Status { get; set; }
    public List<string> Categories { get; set; } = new();
    public int EpisodeCount { get; set; }
}

public class MovieDetailDto : MovieListDto
{
    public string? Description { get; set; }
    public int? Duration { get; set; }

    // SEO Fields
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string OgTitle { get; set; } = string.Empty;
    public string OgDescription { get; set; } = string.Empty;
    public string? OgImage { get; set; }
    public string OgType { get; set; } = "video.movie";
    public string? SchemaMarkup { get; set; }

    public List<EpisodeDto> Episodes { get; set; } = new();
}

public class EpisodeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int EpisodeNumber { get; set; }
    public string? EmbedLink { get; set; }
    public string? FileUrl { get; set; }
    public string? Server { get; set; }
    public string Status { get; set; } = "active";
}

public class CategoryListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MovieCount { get; set; }
    public bool IsFeatured { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public int? Total { get; set; }
    public int? Page { get; set; }
}

// === Homepage DTOs ===
public class HomePageDto
{
    public List<MovieListDto> Featured { get; set; } = new();
    public List<MovieListDto> Trending { get; set; } = new();
    public List<MovieListDto> NewReleases { get; set; } = new();
    public List<MovieListDto> TopRated { get; set; } = new();
    public List<CategorySection> Categories { get; set; } = new();
    public List<MovieListDto> SeriesList { get; set; } = new();
    public List<MovieListDto> SingleMovies { get; set; } = new();
    public BannerDto? HeroBanner { get; set; }
}

public class CategorySection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public List<MovieListDto> Movies { get; set; } = new();
}

public class BannerDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
}