using SunPhim.Models;
using SunPhim.Models.Crawler;

namespace SunPhim.Services.Crawler;

public interface IOphimService
{
    Task<CrawlResult> CrawlMovieListAsync(int page = 1, CancellationToken ct = default);
    Task<CrawlResult> CrawlMovieDetailAsync(string slug, CancellationToken ct = default);
    Task<CrawlResult> CrawlNewUpdatesAsync(int page = 1, CancellationToken ct = default);
    Task<int> SyncAllPagesAsync(int maxPages = 10, CancellationToken ct = default);
}

public class CrawlResult
{
    public bool Success { get; set; }
    public int MoviesProcessed { get; set; }
    public int EpisodesProcessed { get; set; }
    public int NewMovies { get; set; }
    public int UpdatedMovies { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }

    public static CrawlResult Ok(int movies = 0, int episodes = 0, int newMovies = 0, int updated = 0, string? msg = null)
        => new() { Success = true, MoviesProcessed = movies, EpisodesProcessed = episodes, NewMovies = newMovies, UpdatedMovies = updated, Message = msg };

    public static CrawlResult Fail(string error)
        => new() { Success = false, Errors = new() { error } };
}
