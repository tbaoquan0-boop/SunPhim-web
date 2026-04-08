using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunPhim.Data;

namespace SunPhim.Controllers;

[Route("[controller]")]
[ApiController]
public class SitemapController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly ILogger<SitemapController> _log;
    private readonly IConfiguration _config;

    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/2.0";

    public SitemapController(AppDbContext ctx, ILogger<SitemapController> log, IConfiguration config)
    {
        _ctx = ctx;
        _log = log;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetSitemap()
    {
        try
        {
            var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var now = DateTime.UtcNow;

            var movies = await _ctx.Movies
                .AsNoTracking()
                .Where(m => m.IsPublished)
                .Select(m => new { m.Slug, m.UpdatedAt })
                .ToListAsync();

            var categories = await _ctx.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.Slug, c.CreatedAt })
                .ToListAsync();

            var urlSet = new XElement(SitemapNs + "urlset");

            urlSet.Add(CreateUrlEntry(baseUrl, now, "1.0", "daily"));

            foreach (var movie in movies)
            {
                urlSet.Add(CreateUrlEntry(
                    $"{baseUrl}/phim/{movie.Slug}",
                    movie.UpdatedAt,
                    "0.8",
                    "weekly"));
            }

            foreach (var cat in categories)
            {
                urlSet.Add(CreateUrlEntry(
                    $"{baseUrl}/the-loai/{cat.Slug}",
                    cat.CreatedAt,
                    "0.6",
                    "daily"));
            }

            var sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                urlSet);

            Response.ContentType = "application/xml";
            return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error generating sitemap");
            return StatusCode(500, "Error generating sitemap");
        }
    }

    [HttpGet("movies")]
    public async Task<IActionResult> GetMovieSitemap([FromQuery] int page = 1)
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var now = DateTime.UtcNow;
        int pageSize = 1000;

        var movies = await _ctx.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished)
            .OrderBy(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new { m.Slug, m.UpdatedAt })
            .ToListAsync();

        var urlSet = new XElement(SitemapNs + "urlset");
        foreach (var movie in movies)
        {
            urlSet.Add(CreateUrlEntry(
                $"{baseUrl}/phim/{movie.Slug}",
                movie.UpdatedAt,
                "0.8",
                "weekly"));
        }

        Response.ContentType = "application/xml";
        var sitemap = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlSet);
        return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("ror.xml")]
    public async Task<IActionResult> GetRorSitemap()
    {
        var baseUrl = _config["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

        var movies = await _ctx.Movies
            .AsNoTracking()
            .Where(m => m.IsPublished)
            .Take(500)
            .Select(m => new { m.Slug, m.Name })
            .ToListAsync();

        var ror = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(SitemapNs + "rss",
                new XElement(SitemapNs + "channel")));

        var channel = ror.Root!;
        channel.Add(new XElement(SitemapNs + "title", "SunPhim"));
        channel.Add(new XElement(SitemapNs + "link", baseUrl));

        foreach (var m in movies)
        {
            var item = new XElement(SitemapNs + "item");
            item.Add(new XElement(SitemapNs + "link", $"{baseUrl}/phim/{m.Slug}"));
            item.Add(new XElement(SitemapNs + "title", m.Name));
            channel.Add(item);
        }

        Response.ContentType = "application/xml";
        return Content(ror.ToString(), "application/xml", Encoding.UTF8);
    }

    private static XElement CreateUrlEntry(string loc, DateTime lastmod, string priority, string changefreq)
    {
        return new XElement(
            SitemapNs + "url",
            new XElement(SitemapNs + "loc", loc),
            new XElement(SitemapNs + "lastmod", lastmod.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new XElement(SitemapNs + "priority", priority),
            new XElement(SitemapNs + "changefreq", changefreq));
    }
}
