using Microsoft.EntityFrameworkCore;
using SunPhim.Data;
using SunPhim.Helpers;
using SunPhim.Models;
using SunPhim.Models.DTOs;

namespace SunPhim.Services;

public class MovieService : IMovieService
{
    private readonly AppDbContext _context;

    public MovieService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync()
    {
        return await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished)
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Movie?> GetByIdAsync(int id)
    {
        return await _context.Movies
            .AsNoTracking()
            .Include(m => m.Episodes)
            .Include(m => m.MovieCategories)
                .ThenInclude(mc => mc.Category)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Movie?> GetBySlugAsync(string slug)
    {
        return await _context.Movies
            .AsNoTracking()
            .Include(m => m.Episodes.OrderBy(e => e.EpisodeNumber))
            .Include(m => m.MovieCategories)
                .ThenInclude(mc => mc.Category)
            .FirstOrDefaultAsync(m => m.Slug == slug && m.IsPublished);
    }

    public async Task<MovieDetailDto?> GetDetailBySlugAsync(string slug, string baseUrl)
    {
        var movie = await GetBySlugAsync(slug);
        if (movie == null) return null;
        return MapToDetailDto(movie, baseUrl);
    }

    public async Task<IEnumerable<MovieListDto>> GetListDtoAsync(int page = 1, int pageSize = 20)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished)
            .OrderByDescending(m => m.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.MovieCategories)
                .ThenInclude(mc => mc.Category)
            .Include(m => m.Episodes)
            .ToListAsync();

        return movies.Select(m => MapToListDto(m, "")).ToList();
    }

    private static MovieDetailDto MapToDetailDto(Movie m, string baseUrl)
    {
        var dto = new MovieDetailDto
        {
            Id = m.Id,
            Name = m.Name,
            Slug = m.Slug,
            OriginName = m.OriginName,
            Thumb = m.Thumb,
            Poster = m.Poster,
            Year = m.Year,
            Type = m.Type,
            Quality = m.Quality,
            Lang = m.Lang,
            ImdbScore = m.ImdbScore,
            ViewCount = m.ViewCount,
            Status = m.Status,
            Description = m.Description,
            Duration = m.Duration,
            Categories = m.MovieCategories
                .Where(mc => mc.Category != null)
                .Select(mc => mc.Category!.Name)
                .ToList(),
            EpisodeCount = m.Episodes.Count,
            MetaTitle = $"{m.Name} - Xem Phim HD Online | SunPhim",
            MetaDescription = string.IsNullOrWhiteSpace(m.Description)
                ? $"Xem phim {m.Name} ({m.OriginName ?? m.Name}) online chất lượng cao, vietsub, thuyết minh. Cập nhật nhanh nhất."
                : Truncate(m.Description, 160),
            CanonicalUrl = $"{baseUrl}/phim/{m.Slug}",
            OgTitle = m.Name,
            OgDescription = Truncate(m.Description ?? m.Name, 200),
            OgImage = m.Poster ?? m.Thumb,
            Episodes = m.Episodes
                .Where(e => e.Status == "active")
                .OrderBy(e => e.EpisodeNumber)
                .Select(e => new EpisodeDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    EpisodeNumber = e.EpisodeNumber,
                    EmbedLink = e.EmbedLink,
                    FileUrl = e.FileUrl,
                    Server = e.Server,
                    Status = e.Status
                })
                .ToList()
        };

        dto.SchemaMarkup = GenerateSchemaMarkup(m, baseUrl);
        return dto;
    }

    private static string Truncate(string s, int maxLen)
        => s.Length <= maxLen ? s : s[..maxLen];

    private static string GenerateSchemaMarkup(Movie m, string baseUrl)
    {
        var schema = new
        {
            @context = "https://schema.org",
            @type = "Movie",
            name = m.Name,
            description = m.Description,
            image = m.Poster ?? m.Thumb,
            datePublished = m.Year?.ToString(),
            aggregateRating = m.ImdbScore > 0 ? new
            {
                @type = "AggregateRating",
                ratingValue = m.ImdbScore,
                bestRating = 10,
                worstRating = 1
            } : null,
            url = $"{baseUrl}/phim/{m.Slug}"
        };

        return System.Text.Json.JsonSerializer.Serialize(schema,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
    }

    public async Task<IEnumerable<Movie>> GetByTypeAsync(string type)
    {
        return await _context.Movies
            .AsNoTracking()
            .Where(m => m.Type == type && m.IsPublished)
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Movie>> GetByCategoryAsync(string categorySlug)
    {
        return await _context.Movies
            .AsNoTracking()
            .Include(m => m.MovieCategories)
                .ThenInclude(mc => mc.Category)
            .Where(m => m.IsPublished
                && m.MovieCategories.Any(mc => mc.Category!.Slug == categorySlug))
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Movie>> SearchAsync(string keyword)
    {
        return await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished
                && (m.Name.Contains(keyword)
                    || (m.OriginName != null && m.OriginName.Contains(keyword))))
            .OrderByDescending(m => m.ViewCount)
            .ToListAsync();
    }

    public async Task<Movie> CreateAsync(Movie movie)
    {
        movie.CreatedAt = DateTime.UtcNow;
        movie.UpdatedAt = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(movie.Slug))
            movie.Slug = SlugHelper.ToSlug(movie.Name);
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();
        return movie;
    }

    public async Task<Movie?> UpdateAsync(int id, Movie movie)
    {
        var existing = await _context.Movies.FindAsync(id);
        if (existing == null) return null;

        existing.Name = movie.Name;
        existing.Slug = movie.Slug;
        existing.OriginName = movie.OriginName;
        existing.Thumb = movie.Thumb;
        existing.Poster = movie.Poster;
        existing.Year = movie.Year;
        existing.Description = movie.Description;
        existing.Duration = movie.Duration;
        existing.ImdbScore = movie.ImdbScore;
        existing.ViewCount = movie.ViewCount;
        existing.Status = movie.Status;
        existing.Quality = movie.Quality;
        existing.Lang = movie.Lang;
        existing.EmbedLink = movie.EmbedLink;
        existing.FileUrl = movie.FileUrl;
        existing.Source = movie.Source;
        existing.ExternalId = movie.ExternalId;
        existing.IsPublished = movie.IsPublished;
        existing.Type = movie.Type;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null) return false;

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string externalId)
    {
        return await _context.Movies.AnyAsync(m => m.ExternalId == externalId);
    }

    public async Task<HomePageDto> GetHomePageDataAsync(string baseUrl)
    {
        // Khong duoc Task.WhenAll nhieu query tren cung mot DbContext (EF khong thread-safe).
        var featured = await GetFeaturedMoviesAsync(baseUrl, 10);
        var trending = await GetTrendingMoviesAsync(baseUrl, 20);
        var newReleases = await GetNewReleasesAsync(baseUrl, 20);
        var topRated = await GetTopRatedMoviesAsync(baseUrl, 20);
        var categorySections = await GetMoviesByCategoryAsync(baseUrl, 12);
        var series = await GetSeriesMoviesAsync(baseUrl, 20);
        var singles = await GetSingleMoviesAsync(baseUrl, 20);

        return new HomePageDto
        {
            Featured = featured,
            Trending = trending,
            NewReleases = newReleases,
            TopRated = topRated,
            Categories = categorySections,
            SeriesList = series,
            SingleMovies = singles
        };
    }

    public async Task<List<MovieListDto>> GetFeaturedMoviesAsync(string baseUrl, int limit = 10)
    {
        // Lay phim co diem IMDB, neu khong du thi lay phim moi nhat
        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished)
            .OrderByDescending(m => m.ImdbScore > 0)
            .ThenByDescending(m => m.ImdbScore)
            .ThenByDescending(m => m.ViewCount)
            .Take(limit)
            .Include(m => m.MovieCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Episodes)
            .ToListAsync();

        return movies.Select(m => MapToListDto(m, baseUrl)).ToList();
    }

    public async Task<List<MovieListDto>> GetTrendingMoviesAsync(string baseUrl, int limit = 20)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished)
            .OrderByDescending(m => m.ViewCount)
            .Take(limit)
            .Include(m => m.MovieCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Episodes)
            .ToListAsync();

        return movies.Select(m => MapToListDto(m, baseUrl)).ToList();
    }

    public async Task<List<MovieListDto>> GetNewReleasesAsync(string baseUrl, int limit = 20)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Include(m => m.MovieCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Episodes)
            .ToListAsync();

        return movies.Select(m => MapToListDto(m, baseUrl)).ToList();
    }

    public async Task<List<MovieListDto>> GetTopRatedMoviesAsync(string baseUrl, int limit = 20)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished && m.ImdbScore > 0)
            .OrderByDescending(m => m.ImdbScore)
            .Take(limit)
            .Include(m => m.MovieCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Episodes)
            .ToListAsync();

        return movies.Select(m => MapToListDto(m, baseUrl)).ToList();
    }

    public async Task<List<MovieListDto>> GetRandomFeaturedMoviesAsync(string baseUrl, int count = 5)
    {
        // SQL Server khong ho tro EF.Functions.Random() - dung NewId() thay the
        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished)
            .OrderBy(m => Guid.NewGuid())
            .Take(count)
            .Include(m => m.MovieCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Episodes)
            .ToListAsync();

        return movies.Select(m => MapToListDto(m, baseUrl)).ToList();
    }

    public async Task<List<CategorySection>> GetMoviesByCategoryAsync(string baseUrl, int limit = 12)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Take(6)
            .ToListAsync();

        var sections = new List<CategorySection>();
        foreach (var cat in categories)
        {
            var movies = await _context.Movies
                .AsNoTracking()
                .Where(m => m.IsPublished && m.MovieCategories.Any(mc => mc.CategoryId == cat.Id))
                .OrderByDescending(m => m.UpdatedAt)
                .Take(limit)
                .Include(m => m.MovieCategories).ThenInclude(mc => mc.Category)
                .Include(m => m.Episodes)
                .ToListAsync();

            sections.Add(new CategorySection
            {
                Id = cat.Id,
                Name = cat.Name,
                Slug = cat.Slug,
                Movies = movies.Select(m => MapToListDto(m, baseUrl)).ToList()
            });
        }

        return sections;
    }

    public async Task<List<MovieListDto>> GetContinueWatchingAsync(string userId, string baseUrl, int limit = 10)
    {
        var movieIds = await _context.WatchHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.WatchedAt)
            .Select(h => h.MovieId)
            .Distinct()
            .Take(limit)
            .ToListAsync();

        if (movieIds.Count == 0)
            return new List<MovieListDto>();

        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => movieIds.Contains(m.Id))
            .Include(m => m.MovieCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Episodes)
            .ToListAsync();

        // Giu dung thu tu theo WatchedAt (latest first)
        var ordered = movieIds
            .Select(id => movies.FirstOrDefault(m => m.Id == id))
            .Where(m => m != null)
            .Select(m => m!)
            .ToList();

        return ordered.Select(m => MapToListDto(m, baseUrl)).ToList();
    }

    private async Task<List<MovieListDto>> GetSeriesMoviesAsync(string baseUrl, int limit)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished && m.Type == "series")
            .OrderByDescending(m => m.UpdatedAt)
            .Take(limit)
            .Include(m => m.MovieCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Episodes)
            .ToListAsync();

        return movies.Select(m => MapToListDto(m, baseUrl)).ToList();
    }

    private async Task<List<MovieListDto>> GetSingleMoviesAsync(string baseUrl, int limit)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished && m.Type == "single")
            .OrderByDescending(m => m.UpdatedAt)
            .Take(limit)
            .Include(m => m.MovieCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Episodes)
            .ToListAsync();

        return movies.Select(m => MapToListDto(m, baseUrl)).ToList();
    }

    private MovieListDto MapToListDto(Movie m, string baseUrl)
    {
        return new MovieListDto
        {
            Id = m.Id,
            Name = m.Name,
            Slug = m.Slug,
            OriginName = m.OriginName,
            Thumb = m.Thumb,
            Poster = m.Poster,
            Year = m.Year,
            Type = m.Type,
            Quality = m.Quality,
            Lang = m.Lang,
            ImdbScore = m.ImdbScore,
            ViewCount = m.ViewCount,
            Status = m.Status,
            Categories = m.MovieCategories
                .Where(mc => mc.Category != null)
                .Select(mc => mc.Category!.Name)
                .ToList(),
            EpisodeCount = m.Episodes.Count
        };
    }
}
