using Microsoft.AspNetCore.Mvc;
using SunPhim.Services;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EpisodesController : ControllerBase
{
    private readonly EpisodeService _episodeService;

    public EpisodesController(EpisodeService episodeService)
    {
        _episodeService = episodeService;
    }

    [HttpGet("movie/{movieId:int}")]
    public async Task<IActionResult> GetByMovie(int movieId)
    {
        return Ok(await _episodeService.GetByMovieIdAsync(movieId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var episode = await _episodeService.GetByIdAsync(id);
        if (episode == null) return NotFound(new { message = "Không tìm thấy tập phim" });
        return Ok(episode);
    }

    [HttpGet("movie/{movieId:int}/episode/{episodeNumber:int}")]
    public async Task<IActionResult> GetByMovieAndNumber(int movieId, int episodeNumber)
    {
        var episode = await _episodeService.GetByMovieAndNumberAsync(movieId, episodeNumber);
        if (episode == null) return NotFound(new { message = "Không tìm thấy tập phim" });
        return Ok(episode);
    }

    [HttpPost("{id:int}/view")]
    public async Task<IActionResult> IncrementView(int id)
    {
        await _episodeService.IncrementViewAsync(id);
        return Ok(new { message = "Đã tăng lượt xem" });
    }
}
