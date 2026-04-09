using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SunPhim.Data;
using SunPhim.Services.Crawler;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/crawler")]
public class CrawlerController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly INguonCService _nguonc;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<CrawlerController> _log;

    public CrawlerController(
        AppDbContext ctx,
        INguonCService nguonc,
        ISchedulerFactory schedulerFactory,
        ILogger<CrawlerController> log)
    {
        _ctx = ctx;
        _nguonc = nguonc;
        _schedulerFactory = schedulerFactory;
        _log = log;
    }

    /// <summary>Xoa tat ca phim (episodes, movie_categories, movies) roi re-import tu NguonC.</summary>
    [HttpDelete("reset")]
    public async Task<IActionResult> ResetAndReimport([FromQuery] int pages = 5, [FromQuery] bool startCrawl = true)
    {
        try
        {
            _log.LogInformation("Bat dau xoa toan bo phim (reset + reimport)...");

            var epCount = await _ctx.Episodes.CountAsync();
            var mcCount = await _ctx.MovieCategories.CountAsync();
            var mvCount = await _ctx.Movies.CountAsync();

            await _ctx.Episodes.ExecuteDeleteAsync();
            await _ctx.MovieCategories.ExecuteDeleteAsync();
            await _ctx.Movies.ExecuteDeleteAsync();

            _log.LogInformation("Da xoa: {Ep} episodes, {Mc} movie_categories, {Mv} movies", epCount, mcCount, mvCount);

            if (!startCrawl)
                return Ok(new
                {
                    deleted = new { episodes = epCount, movieCategories = mcCount, movies = mvCount },
                    crawlStarted = false,
                    message = "Da xoa xong. Bat dau crawl thu cong bang /api/crawler/sync hoac /api/crawler/run-now"
                });

            // Bat dau crawl tu trang 1
            int synced = 0;
            int totalNew = 0, totalUpd = 0, totalEp = 0;

            for (int page = 1; page <= pages; page++)
            {
                var r = await _nguonc.CrawlNewUpdatesAsync(page);
                if (r.Success)
                {
                    totalNew += r.NewMovies;
                    totalUpd += r.UpdatedMovies;
                    totalEp += r.EpisodesProcessed;
                    synced++;
                }
                if (r.Errors.FirstOrDefault()?.Contains("Khong lay duoc") == true)
                    break;
                await Task.Delay(500);
            }

            return Ok(new
            {
                deleted = new { episodes = epCount, movieCategories = mcCount, movies = mvCount },
                crawled = new { pages = synced, newMovies = totalNew, updated = totalUpd, episodes = totalEp },
                message = $"Da xoa {mvCount} phim + reimport {synced} trang: {totalNew} moi, {totalUpd} cap nhat, {totalEp} tap moi"
            });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Reset + Reimport that bai");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromQuery] int page = 1)
    {
        var result = await _nguonc.CrawlNewUpdatesAsync(page);
        return Ok(result);
    }

    [HttpPost("sync/detail/{slug}")]
    public async Task<IActionResult> SyncDetail(string slug)
    {
        var result = await _nguonc.CrawlMovieDetailAsync(slug);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("sync/all")]
    public async Task<IActionResult> SyncAll([FromQuery] int maxPages = 10)
    {
        var count = await _nguonc.SyncAllPagesAsync(maxPages);
        return Ok(new { syncedPages = count, message = $"Đã sync {count} trang (NguonC API)" });
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

    [HttpPost("run-now")]
    public async Task<IActionResult> RunNow([FromQuery] int maxPages = 3)
    {
        try
        {
            _log.LogInformation("Manual crawl NguonC triggered via /api/crawler/run-now");

            var result = await _nguonc.CrawlNewUpdatesAsync(1);
            if (!result.Success)
                return BadRequest(result);

            int syncedPages = 1;
            if (maxPages > 1)
            {
                syncedPages += await _nguonc.SyncPagesAsync(2, maxPages);
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                syncedPages,
                summary = new
                {
                    newMovies = result.NewMovies,
                    updated = result.UpdatedMovies,
                    episodes = result.EpisodesProcessed
                }
            });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Manual crawl failed");
            return StatusCode(500, new
            {
                success = false,
                message = "Crawl thủ công thất bại: " + ex.Message,
                detail = ex.StackTrace
            });
        }
    }
}
