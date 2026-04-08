using Microsoft.AspNetCore.Mvc;
using SunPhim.Services;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type)
    {
        if (!string.IsNullOrEmpty(type))
            return Ok(await _movieService.GetByTypeAsync(type));
        return Ok(await _movieService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var movie = await _movieService.GetByIdAsync(id);
        if (movie == null) return NotFound(new { message = "Không tìm thấy phim" });
        return Ok(movie);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var movie = await _movieService.GetBySlugAsync(slug);
        if (movie == null) return NotFound(new { message = "Không tìm thấy phim" });
        return Ok(movie);
    }

    [HttpGet("category/{categorySlug}")]
    public async Task<IActionResult> GetByCategory(string categorySlug)
    {
        return Ok(await _movieService.GetByCategoryAsync(categorySlug));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Từ khóa không hợp lệ" });

        return Ok(await _movieService.SearchAsync(keyword));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Models.Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.Name) || string.IsNullOrWhiteSpace(movie.Slug))
            return BadRequest(new { message = "Tên và Slug là bắt buộc" });

        var created = await _movieService.CreateAsync(movie);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Models.Movie movie)
    {
        var updated = await _movieService.UpdateAsync(id, movie);
        if (updated == null) return NotFound(new { message = "Không tìm thấy phim" });
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _movieService.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "Không tìm thấy phim" });
        return NoContent();
    }
}
