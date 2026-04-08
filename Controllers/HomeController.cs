using Microsoft.AspNetCore.Mvc;
using SunPhim.Models.DTOs;
using SunPhim.Services;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly ICategoryService _categoryService;
    private readonly IAdService _adService;
    private readonly IConfiguration _config;

    public HomeController(
        IMovieService movieService,
        ICategoryService categoryService,
        IAdService adService,
        IConfiguration config)
    {
        _movieService = movieService;
        _categoryService = categoryService;
        _adService = adService;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetHomePage()
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var home = await _movieService.GetHomePageDataAsync(baseUrl);
        return Ok(home);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured()
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var movies = await _movieService.GetFeaturedMoviesAsync(baseUrl, 10);
        return Ok(movies);
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int limit = 20)
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var movies = await _movieService.GetTrendingMoviesAsync(baseUrl, limit);
        return Ok(movies);
    }

    [HttpGet("new-releases")]
    public async Task<IActionResult> GetNewReleases([FromQuery] int limit = 20)
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var movies = await _movieService.GetNewReleasesAsync(baseUrl, limit);
        return Ok(movies);
    }

    [HttpGet("top-rated")]
    public async Task<IActionResult> GetTopRated([FromQuery] int limit = 20)
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var movies = await _movieService.GetTopRatedMoviesAsync(baseUrl, limit);
        return Ok(movies);
    }

    [HttpGet("by-category")]
    public async Task<IActionResult> GetByCategory([FromQuery] int limit = 12)
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var sections = await _movieService.GetMoviesByCategoryAsync(baseUrl, limit);
        return Ok(sections);
    }

    [HttpGet("random-featured")]
    public async Task<IActionResult> GetRandomFeatured([FromQuery] int count = 5)
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var movies = await _movieService.GetRandomFeaturedMoviesAsync(baseUrl, count);
        return Ok(movies);
    }

    [HttpGet("continue-watching")]
    public async Task<IActionResult> GetContinueWatching([FromQuery] string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Ok(new List<MovieListDto>());

        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var movies = await _movieService.GetContinueWatchingAsync(userId, baseUrl, 10);
        return Ok(movies);
    }

    [HttpGet("hero-banner")]
    public async Task<IActionResult> GetHeroBanner()
    {
        var banner = await _adService.GetActiveBannerAsync("hero");
        return Ok(banner);
    }
}
