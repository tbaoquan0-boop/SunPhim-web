using Microsoft.AspNetCore.Mvc;
using SunPhim.Models.Crawler;
using SunPhim.Services.Crawler;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/nguonc")]
public class NguonCController : ControllerBase
{
    private readonly INguonCService _nguonc;
    private readonly ILogger<NguonCController> _log;

    public NguonCController(INguonCService nguonc, ILogger<NguonCController> log)
    {
        _nguonc = nguonc;
        _log = log;
    }

    [HttpPost("crawl/{slug}")]
    public async Task<IActionResult> CrawlDetail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new { error = "Slug khong duoc trong" });

        var result = await _nguonc.CrawlMovieDetailAsync(slug);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("crawl/page")]
    public async Task<IActionResult> CrawlPage([FromQuery] string? url = null)
    {
        url ??= "https://phim.nguonc.com/";

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            return BadRequest(new { error = "URL khong hop le" });

        var result = await _nguonc.CrawlListPageAsync(url);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// MOT LAN NHAN — lay TAT CA phim (FULL, khong gioi han trang).
    /// Chi danh muc: Phim bo (1338 trang), Phim le, Phim chieu rap.
    /// Phim moi cap nhat chi lay trang 1 (khoang 10 phim moi nhat).
    /// </summary>
    [HttpPost("crawl/all")]
    [HttpPost("crawl/full")]
    public async Task<IActionResult> CrawlAll()
    {
        _log.LogInformation("CrawlAll FULL started — lay TAT CA phim (khong gioi han trang).");

        var lists = new (string name, string endpoint)[]
        {
            ("Phim bo",        NguonCApiPaths.DanhSach("phim-bo")),
            ("Phim le",        NguonCApiPaths.DanhSach("phim-le")),
            ("Phim chieu rap", NguonCApiPaths.DanhSach("phim-chieu-rap")),
        };

        int grandNew = 0, grandUpd = 0, grandEp = 0;
        var errors = new List<string>();

        foreach (var item in lists)
        {
            Console.WriteLine($"\n========== CrawlAll FULL: {item.name} ==========");
            var r = await _nguonc.CrawlFullDanhSachAsync(item.endpoint, HttpContext.RequestAborted);

            if (r.Success)
            {
                grandNew += r.NewMovies;
                grandUpd += r.UpdatedMovies;
                grandEp += r.EpisodesProcessed;
            }
            else
            {
                errors.Add($"{item.name}: {r.Errors.FirstOrDefault() ?? r.Message}");
            }
        }

        return Ok(new
        {
            success = errors.Count == 0,
            summary = new
            {
                newMovies = grandNew,
                updated   = grandUpd,
                episodes  = grandEp,
                errors
            }
        });
    }

    /// <summary>Phim bo / le / chieu rap — API danh-sach (gioi han trang).</summary>
    [HttpPost("crawl/home")]
    public async Task<IActionResult> CrawlHome([FromQuery] int? maxPagesPerCategory = null)
    {
        _log.LogInformation("Crawl home categories (NguonC API)...");

        var categories = new[]
        {
            ("phim-bo", "phim-bo"),
            ("phim-le", "phim-le"),
            ("phim-chieu-rap", "phim-chieu-rap"),
        };

        int totalNew = 0, totalUpd = 0, totalEp = 0;
        var errors = new List<string>();

        foreach (var (name, listSlug) in categories)
        {
            Console.WriteLine($"\n=== Crawl API danh muc: {name} ===");
            var result = await _nguonc.CrawlDanhSachAsync(listSlug, maxPagesPerCategory);
            if (result.Success)
            {
                totalNew += result.NewMovies;
                totalUpd += result.UpdatedMovies;
                totalEp += result.EpisodesProcessed;
            }
            else
            {
                errors.Add($"{name}: {result.Errors.FirstOrDefault() ?? result.Message}");
            }
        }

        return Ok(new
        {
            success = errors.Count == 0,
            summary = new
            {
                newMovies = totalNew,
                updated = totalUpd,
                episodes = totalEp,
                errors
            }
        });
    }

    /// <summary>GET /api/films/nam-phat-hanh/{year} — toan bo trang hoac gioi han maxPages.</summary>
    [HttpPost("crawl/year/{year:int}")]
    public async Task<IActionResult> CrawlYear(int year, [FromQuery] int? maxPages = null)
    {
        if (year < 1900 || year > 2100)
            return BadRequest(new { error = "Nam khong hop le" });

        var result = await _nguonc.CrawlNamPhatHanhAsync(year, maxPages);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Lan luot tat ca nam tu maxYear xuong minYear (mac dinh den nam hien tai).</summary>
    [HttpPost("crawl/years")]
    public async Task<IActionResult> CrawlAllYears(
        [FromQuery] int minYear = 1990,
        [FromQuery] int? maxYear = null,
        [FromQuery] int? maxPagesPerYear = null)
    {
        int hi = maxYear ?? DateTime.UtcNow.Year;
        if (hi < minYear)
            return BadRequest(new { error = "maxYear phai >= minYear" });

        _log.LogInformation("Crawl all years {Min}..{Max}, maxPagesPerYear={Cap}", minYear, hi, maxPagesPerYear);
        var result = await _nguonc.CrawlAllYearsAsync(minYear, hi, maxPagesPerYear);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>GET /api/films/the-loai/{slug}</summary>
    [HttpPost("crawl/the-loai/{slug}")]
    public async Task<IActionResult> CrawlTheLoai(string slug, [FromQuery] int? maxPages = null)
    {
        var result = await _nguonc.CrawlTheLoaiAsync(slug, maxPages);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>GET /api/films/quoc-gia/{slug}</summary>
    [HttpPost("crawl/quoc-gia/{slug}")]
    public async Task<IActionResult> CrawlQuocGia(string slug, [FromQuery] int? maxPages = null)
    {
        var result = await _nguonc.CrawlQuocGiaAsync(slug, maxPages);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>GET /api/films/search?keyword=</summary>
    [HttpPost("crawl/search")]
    public async Task<IActionResult> CrawlSearch([FromQuery] string keyword, [FromQuery] int? maxPages = null)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { error = "keyword bat buoc" });

        var result = await _nguonc.CrawlSearchAsync(keyword, maxPages);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
