using Microsoft.EntityFrameworkCore;
using Quartz;
using SunPhim.Data;
using SunPhim.Models.Crawler;
using SunPhim.Services.Crawler;

namespace SunPhim.Jobs;

[DisallowConcurrentExecution]
public class NguonCCrawlJob : IJob
{
    private readonly INguonCService _nguonc;
    private readonly ILogger<NguonCCrawlJob> _log;

    public NguonCCrawlJob(INguonCService nguonc, ILogger<NguonCCrawlJob> log)
    {
        _nguonc = nguonc;
        _log = log;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var jobType = context.MergedJobDataMap.GetString("JobType") ?? "incremental";
        _log.LogInformation("NguonCCrawlJob started: {JobType}", jobType);

        try
        {
            CrawlResult result;
            switch (jobType)
            {
                case "full":
                {
                    // Lay tat ca phim: Phim bo + Phim le + Phim chieu rap (khong gioi han trang)
                    var lists = new[] { "phim-bo", "phim-le", "phim-chieu-rap" };
                    int totalNew = 0, totalUpd = 0, totalEp = 0;
                    foreach (var list in lists)
                    {
                        _log.LogInformation("Crawl full danh muc: {List}", list);
                        var r = await _nguonc.CrawlFullDanhSachAsync(
                            NguonCApiPaths.DanhSach(list), context.CancellationToken);
                        if (r.Success)
                        {
                            totalNew += r.NewMovies;
                            totalUpd += r.UpdatedMovies;
                            totalEp += r.EpisodesProcessed;
                        }
                    }
                    result = CrawlResult.Ok(newMovies: totalNew, updated: totalUpd, episodes: totalEp,
                        msg: $"Full crawl: {totalNew} moi, {totalUpd} cap nhat, {totalEp} tap");
                    break;
                }
                case "incremental":
                {
                    // Chi lay 1 trang phim moi cap nhat
                    var page = int.TryParse(context.MergedJobDataMap.GetString("Page") ?? "1", out var p) ? p : 1;
                    result = await _nguonc.CrawlNewUpdatesAsync(page, context.CancellationToken);
                    break;
                }
                default:
                    result = CrawlResult.Fail($"Unknown jobType: {jobType}");
                    break;
            }

            _log.LogInformation("NguonCCrawlJob completed: {Message}", result.Message);
            context.Result = result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "NguonCCrawlJob failed");
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
