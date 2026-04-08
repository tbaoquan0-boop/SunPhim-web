using Microsoft.AspNetCore.Mvc;
using SunPhim.Services;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EpisodesController : ControllerBase
{
    private readonly IEpisodeService _episodeService;

    public EpisodesController(IEpisodeService episodeService)
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Models.Episode episode)
    {
        if (episode.MovieId <= 0)
            return BadRequest(new { message = "MovieId không hợp lệ" });

        var created = await _episodeService.CreateAsync(episode);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Models.Episode episode)
    {
        var updated = await _episodeService.UpdateAsync(id, episode);
        if (updated == null) return NotFound(new { message = "Không tìm thấy tập phim" });
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _episodeService.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "Không tìm thấy tập phim" });
        return NoContent();
    }

    [HttpPost("{id:int}/view")]
    public async Task<IActionResult> IncrementView(int id)
    {
        await _episodeService.IncrementViewAsync(id);
        return Ok(new { message = "Đã tăng lượt xem" });
    }
}
