using Microsoft.AspNetCore.Mvc;
using Quartz;
using SunPhim.Services.Crawler;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/crawler")]
public class CrawlerController : ControllerBase
{
    private readonly IOphimService _ophim;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<CrawlerController> _log;

    public CrawlerController(
        IOphimService ophim,
        ISchedulerFactory schedulerFactory,
        ILogger<CrawlerController> log)
    {
        _ophim = ophim;
        _schedulerFactory = schedulerFactory;
        _log = log;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromQuery] int page = 1)
    {
        var result = await _ophim.CrawlNewUpdatesAsync(page);
        return Ok(result);
    }

    [HttpPost("sync/detail/{slug}")]
    public async Task<IActionResult> SyncDetail(string slug)
    {
        var result = await _ophim.CrawlMovieDetailAsync(slug);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("sync/all")]
    public async Task<IActionResult> SyncAll([FromQuery] int maxPages = 10)
    {
        var count = await _ophim.SyncAllPagesAsync(maxPages);
        return Ok(new { syncedPages = count, message = $"Đã sync {count} trang" });
    }

    [HttpPost("trigger/{jobName}")]
    public async Task<IActionResult> TriggerJob(string jobName)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.TriggerJob(new JobKey(jobName));
            return Ok(new { message = $"Job '{jobName}' đã được kích hoạt" });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to trigger job {JobName}", jobName);
            return BadRequest(new { error = ex.Message });
        }
    }
}
