using Microsoft.AspNetCore.Mvc;
using SunPhim.Models.DTOs;
using SunPhim.Services;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly MovieService _movieService;
    private readonly IConfiguration _config;

    public MoviesController(MovieService movieService, IConfiguration config)
    {
        _movieService = movieService;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type)
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        if (!string.IsNullOrEmpty(type))
        {
            return Ok(await _movieService.GetByTypeAsync(type, baseUrl));
        }
        return Ok(await _movieService.GetListDtoAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var movie = await _movieService.GetByIdAsync(id);
        if (movie == null) return NotFound(new { message = "Không tìm thấy phim" });
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var detail = await _movieService.GetDetailBySlugAsync(movie.Slug, baseUrl);
        return Ok(detail);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var movie = await _movieService.GetDetailBySlugAsync(slug, baseUrl);
        if (movie == null) return NotFound(new { message = "Không tìm thấy phim" });
        return Ok(movie);
    }

    [HttpGet("category/{categorySlug}")]
    public async Task<IActionResult> GetByCategory(string categorySlug)
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        return Ok(await _movieService.GetByCategoryAsync(categorySlug, baseUrl));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Từ khóa không hợp lệ" });
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        return Ok(await _movieService.SearchAsync(keyword, baseUrl));
    }
}
