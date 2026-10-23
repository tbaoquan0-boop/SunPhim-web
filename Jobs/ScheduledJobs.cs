using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SunPhim.Services.Crawler;

namespace SunPhim.Jobs;

public class NguonCCrawlerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NguonCCrawlerBackgroundService> _log;

    public NguonCCrawlerBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NguonCCrawlerBackgroundService> log)
    {
        _serviceProvider = serviceProvider;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("NguonC Crawler Background Service đã khởi động.");

        // Chờ 15s sau khi app khởi động rồi chạy lần crawl đầu tiên
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        // Chạy lần đầu
        await RunIncrementalCrawlAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTime.UtcNow.AddHours(7); // Giờ Việt Nam (UTC+7)

            // Chủ nhật lúc 3h sáng: Chạy full crawl
            if (now.DayOfWeek == DayOfWeek.Sunday && now.Hour == 3)
            {
                await RunFullCrawlAsync(stoppingToken);
            }
            else
            {
                await RunIncrementalCrawlAsync(stoppingToken);
            }
        }
    }

    private async Task RunIncrementalCrawlAsync(CancellationToken ct)
    {
        try
        {
            _log.LogInformation("Bắt đầu crawl cập nhật (incremental)...");
            using var scope = _serviceProvider.CreateScope();
            var nguonc = scope.ServiceProvider.GetRequiredService<INguonCService>();
            var result = await nguonc.CrawlNewUpdatesAsync(1, ct);
            _log.LogInformation("Crawl hoàn tất: {Message}", result.Message);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _log.LogError(ex, "Lỗi trong quá trình crawl cập nhật");
        }
    }

    private async Task RunFullCrawlAsync(CancellationToken ct)
    {
        try
        {
            _log.LogInformation("Bắt đầu full crawl tuần...");
            using var scope = _serviceProvider.CreateScope();
            var nguonc = scope.ServiceProvider.GetRequiredService<INguonCService>();
            var lists = new[] { "phim-bo", "phim-le", "phim-chieu-rap" };
            int totalNew = 0, totalUpd = 0, totalEp = 0;

            foreach (var list in lists)
            {
                var r = await nguonc.CrawlFullDanhSachAsync(NguonCApiPaths.DanhSach(list), ct);
                if (r.Success)
                {
                    totalNew += r.NewMovies;
                    totalUpd += r.UpdatedMovies;
                    totalEp += r.EpisodesProcessed;
                }
            }

            _log.LogInformation("Full crawl hoàn tất: {New} mới, {Upd} cập nhật, {Ep} tập", totalNew, totalUpd, totalEp);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _log.LogError(ex, "Lỗi trong quá trình full crawl");
        }
    }
}
