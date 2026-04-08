using Microsoft.EntityFrameworkCore;
using Quartz;
using SunPhim.Data;
using SunPhim.Services.Crawler;

namespace SunPhim.Jobs;

[DisallowConcurrentExecution]
public class OphimCrawlJob : IJob
{
    private readonly IOphimService _ophim;
    private readonly ILogger<OphimCrawlJob> _log;

    public OphimCrawlJob(IOphimService ophim, ILogger<OphimCrawlJob> log)
    {
        _ophim = ophim;
        _log = log;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var jobType = context.MergedJobDataMap.GetString("JobType") ?? "incremental";
        _log.LogInformation("OphimCrawlJob started: {JobType}", jobType);

        try
        {
            CrawlResult result;
            if (jobType == "full")
            {
                var synced = await _ophim.SyncAllPagesAsync(10);
                result = CrawlResult.Ok(msg: $"Synced {synced} pages");
            }
            else
            {
                var page = int.TryParse(context.MergedJobDataMap.GetString("Page") ?? "1", out var p) ? p : 1;
                result = await _ophim.CrawlNewUpdatesAsync(page);
            }

            _log.LogInformation("OphimCrawlJob completed: {Message}", result.Message);
            context.Result = result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "OphimCrawlJob failed");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}

[DisallowConcurrentExecution]
public class FakeViewJob : IJob
{
    private readonly AppDbContext _ctx;
    private readonly ILogger<FakeViewJob> _log;
    private static readonly Random Rng = new();

    public FakeViewJob(AppDbContext ctx, ILogger<FakeViewJob> log)
    {
        _ctx = ctx;
        _log = log;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _log.LogInformation("FakeViewJob started");

        try
        {
            var movies = await _ctx.Movies
                .Where(m => m.IsPublished && m.ViewCount > 0)
                .OrderByDescending(m => m.UpdatedAt)
                .Take(100)
                .ToListAsync(context.CancellationToken);

            foreach (var movie in movies)
            {
                var boost = Rng.Next(5, 21);
                movie.ViewCount += boost;
            }

            await _ctx.SaveChangesAsync(context.CancellationToken);
            _log.LogInformation("FakeViewJob completed: boosted {Count} movies", movies.Count);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "FakeViewJob failed");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
