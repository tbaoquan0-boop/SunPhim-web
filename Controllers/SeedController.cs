using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunPhim.Data;
using SunPhim.Models;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private const string DemoEmbed = "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ";

    private readonly AppDbContext _ctx;
    private readonly ILogger<SeedController> _log;

    public SeedController(AppDbContext ctx, ILogger<SeedController> log)
    {
        _ctx = ctx;
        _log = log;
    }

    /// <summary>
    /// Poster TMDB hop le (image.tmdb.org) — thumb/poster giong file, khac size.
    /// </summary>
    private static string TmdbThumb(string posterFile)
        => $"https://image.tmdb.org/t/p/w342/{posterFile.TrimStart('/')}";

    private static string TmdbPoster(string posterFile)
        => $"https://image.tmdb.org/t/p/w1280/{posterFile.TrimStart('/')}";

    [HttpPost]
    public async Task<IActionResult> SeedData()
    {
        if (await _ctx.Movies.AnyAsync())
            return Ok(new { message = "DB đã có dữ liệu, bỏ qua seed.", count = await _ctx.Movies.CountAsync() });

        var cats = await SeedCategoriesAsync();
        var movies = await SeedMoviesAsync(cats);
        await SeedEpisodesAsync(movies);

        _log.LogInformation("Seed completed: {CatCount} categories, {MovieCount} movies",
            cats.Count, movies.Count);

        return Ok(new
        {
            message = "Seed thành công!",
            categories = cats.Count,
            movies = movies.Count,
            note = "Reload trang web để xem kết quả. Ảnh dùng TMDB CDN."
        });
    }

    [HttpDelete]
    public async Task<IActionResult> ClearData()
    {
        var count = await _ctx.Movies.CountAsync();
        _ctx.Movies.RemoveRange(_ctx.Movies);
        _ctx.Episodes.RemoveRange(_ctx.Episodes);
        _ctx.Categories.RemoveRange(_ctx.Categories);
        _ctx.MovieCategories.RemoveRange(_ctx.MovieCategories);
        await _ctx.SaveChangesAsync();
        return Ok(new { message = $"Đã xóa {count} phim." });
    }

    private async Task<List<Category>> SeedCategoriesAsync()
    {
        var cats = new[]
        {
            new Category { Name = "Hành Động",     Slug = "hanh-dong",       SortOrder = 1, IsActive = true },
            new Category { Name = "Hài Hước",     Slug = "hai-huoc",        SortOrder = 2, IsActive = true },
            new Category { Name = "Tình Cảm",      Slug = "tinh-cam",        SortOrder = 3, IsActive = true },
            new Category { Name = "Kinh Dị",      Slug = "kinh-di",         SortOrder = 4, IsActive = true },
            new Category { Name = "Khoa Học",     Slug = "khoa-hoc",        SortOrder = 5, IsActive = true },
            new Category { Name = "Phiêu Lưu",    Slug = "phieu-luu",       SortOrder = 6, IsActive = true },
        };
        _ctx.Categories.AddRange(cats);
        await _ctx.SaveChangesAsync();
        return cats.ToList();
    }

    private async Task<List<Movie>> SeedMoviesAsync(List<Category> cats)
    {
        var movies = new List<Movie>();
        var movieData = GetSeedMovieData();

        foreach (var d in movieData)
        {
            var m = new Movie
            {
                Name = d.Name,
                Slug = d.Slug,
                OriginName = d.OriginName,
                Thumb = d.Thumb,
                Poster = d.Poster,
                Year = d.Year,
                Description = d.Description,
                Duration = d.Duration,
                ImdbScore = d.ImdbScore,
                ViewCount = d.ViewCount,
                Status = d.Status,
                Quality = d.Quality,
                Lang = d.Lang,
                Type = d.Type,
                Source = "seed",
                ExternalId = "seed-" + d.Slug,
                IsPublished = true
            };
            _ctx.Movies.Add(m);
            movies.Add(m);
        }

        await _ctx.SaveChangesAsync();

        foreach (var m in movies)
        {
            var catIndex = movies.IndexOf(m) % cats.Count;
            _ctx.MovieCategories.Add(new MovieCategory { MovieId = m.Id, CategoryId = cats[catIndex].Id });
        }

        await _ctx.SaveChangesAsync();
        return movies;
    }

    private async Task SeedEpisodesAsync(List<Movie> movies)
    {
        var rng = new Random();
        foreach (var m in movies)
        {
            if (m.Type == "series")
            {
                var epCount = rng.Next(6, 21);
                for (int i = 1; i <= epCount; i++)
                {
                    _ctx.Episodes.Add(new Episode
                    {
                        MovieId = m.Id,
                        Name = $"Tập {i}",
                        EpisodeNumber = i,
                        Server = "Demo",
                        EmbedLink = DemoEmbed,
                        Status = "active"
                    });
                }
            }
            else
            {
                // Phim le: 1 tap embed de nut "Xem phim" / watch.html hoat dong
                _ctx.Episodes.Add(new Episode
                {
                    MovieId = m.Id,
                    Name = "Xem phim",
                    EpisodeNumber = 1,
                    Server = "Demo",
                    EmbedLink = DemoEmbed,
                    Status = "active"
                });
            }
        }

        await _ctx.SaveChangesAsync();
    }

    private static List<SeedMovieRow> GetSeedMovieData()
    {
        return new List<SeedMovieRow>
        {
            new("Avengers: Endgame", "avengers-endgame", "Avengers: Endgame", 2019, 8.4, 980000, "completed", "FullHD", "Vietsub", "single", 181,
                "Sau sự kiện Thanos tiêu diệt một nửa vũ trụ, các Avengers còn sống sót phải tìm cách đảo ngược thảm họa.",
                "or06FN3Dka5tukK1e9a16nB4ixh.jpg"),
            new("Spider-Man: No Way Home", "spider-man-no-way-home", "Spider-Man: No Way Home", 2021, 8.2, 870000, "completed", "FullHD", "Vietsub", "single", 148,
                "Peter Parker nhờ Doctor Strange xóa danh tính của mình nhưng lỡ làm phép sai lầm, mở cánh cửa đa vũ trụ.",
                "1g0dhYtq4irTY1GPXvft6k4YLjm.jpg"),
            new("The Dark Knight", "the-dark-knight", "The Dark Knight", 2008, 9.0, 750000, "completed", "FullHD", "Vietsub", "single", 152,
                "Batman đối mặt với Joker - kẻ khủng bố điên loạn đang gây hỗn loạn Gotham City.",
                "qJ2tW6WMUDux911r6m7haRef0WH.jpg"),
            new("Interstellar", "interstellar", "Interstellar", 2014, 8.6, 620000, "completed", "FullHD", "Vietsub", "single", 169,
                "Một nhóm nhà thám hiểm du hành qua lỗ sâu để tìm kiếm hành tinh mới cho loài người.",
                "gEU2QniE6E77NI6lCU6MxlNBvIx.jpg"),
            new("Inception", "inception", "Inception", 2010, 8.8, 580000, "completed", "FullHD", "Vietsub", "single", 148,
                "Một kẻ trộm chuyên đánh cắp bí mật qua giấc mơ bị giao nhiệm vụ ngược lại - cắm một ý tưởng vào tâm trí ai đó.",
                "9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg"),
            new("Parasite", "parasite", "Gisaengchung", 2019, 8.5, 510000, "completed", "FullHD", "Vietsub", "single", 132,
                "Một gia đình nghèo âm mưu trở thành người giúp việc và gia sư cho một gia đình giàu có.",
                "7IiTTgloJzvGI1TAYymCfbfl3vT.jpg"),
            new("Joker", "joker-2019", "Joker", 2019, 8.4, 490000, "completed", "FullHD", "Vietsub", "single", 122,
                "Câu chuyện nguồn gốc của Joker - kẻ phản diện huyền thoại Gotham.",
                "udDclJoHjfjb8EkgsdBFDXxJjAL.jpg"),
            new("Oppenheimer", "oppenheimer", "Oppenheimer", 2023, 8.6, 450000, "completed", "FullHD", "Vietsub", "single", 180,
                "Câu chuyện về J. Robert Oppenheimer, nhà vật lý đứng sau việc chế tạo bom nguyên tử đầu tiên.",
                "8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg"),
            new("Dune: Part Two", "dune-part-two", "Dune: Part Two", 2024, 8.8, 430000, "completed", "FullHD", "Vietsub", "single", 166,
                "Paul Atreides hợp nhất với Fremen để trả thù những kẻ đã hủy diệt gia đình mình.",
                "8b8R8l48QJoLWqkomAt6XNxczL4.jpg"),
            new("The Conjuring", "the-conjuring", "The Conjuring", 2013, 7.5, 380000, "completed", "FullHD", "Vietsub", "single", 112,
                "Ed và Lorraine Warren đối mặt với linh hồn quỷ dữ trong ngôi nhà bị ám.",
                "wqnzpJR3PxmkPiETGNNJZqjNhs7.jpg"),
            new("Squid Game", "squid-game", "Squid Game", 2021, 8.0, 920000, "completed", "FullHD", "Vietsub", "series", 60,
                "Hàng trăm người chơi kẹt tiền nhận lời mời tham gia một cuộc chơi sinh tồn với giải thưởng khổng lồ.",
                "dDlEmu3EZ0Pgg93K2SVNLCjCSvE.jpg"),
            new("Wednesday", "wednesday", "Wednesday", 2022, 8.1, 850000, "completed", "FullHD", "Vietsub", "series", 47,
                "Wednesday Addams theo học tại học viện Nevermore.",
                "jeGtaGwGcVe0EiBslzjdzK7N2b6.jpg"),
            new("Stranger Things", "stranger-things", "Stranger Things", 2016, 8.7, 790000, "ongoing", "FullHD", "Vietsub", "series", 51,
                "Một nhóm trẻ vùng Indiana khám phá bí mật siêu nhiên.",
                "49WJfeN0moxb9IPfGn8AIqMGskD.jpg"),
            new("Breaking Bad", "breaking-bad", "Breaking Bad", 2008, 9.5, 680000, "completed", "FullHD", "Vietsub", "series", 49,
                "Một giáo viên hóa học chuyển sang nấu meth sau khi được chẩn đoán ung thư phổi.",
                "ggFH2NuYjjA31hJc4jxzWfk2aDl.jpg"),
            new("Viking: Valhalla", "viking-valhalla", "Vikings: Valhalla", 2022, 8.4, 520000, "ongoing", "FullHD", "Vietsub", "series", 45,
                "Phần tiếp theo của series Vikings.",
                "1umWHAPCzSg9Z4SyNv6J2mePlJ.jpg"),
            new("All of Us Are Dead", "all-of-us-are-dead", "All of Us Are Dead", 2022, 7.5, 470000, "completed", "FullHD", "Vietsub", "series", 55,
                "Học sinh chiến đấu sinh tồn khi zombie tấn công trường học.",
                "wmnDU1aU9fw1BVB6VexEqrbprVM.jpg"),
            new("Money Heist", "money-heist", "La Casa de Papel", 2017, 8.2, 640000, "completed", "FullHD", "Vietsub", "series", 70,
                "Nhóm tội phạm đột nhập nhà máy in tiền.",
                "reEMJA1uzscCbkpeRJeTT2bjqUp.jpg"),
            new("The Glory", "the-glory", "The Glory", 2022, 8.3, 590000, "completed", "FullHD", "Vietsub", "series", 60,
                "Trả thù những kẻ bạo lực học đường.",
                "7IiTTgloJzvGI1TAYymCfbfl3vT.jpg"),
        };
    }

    /// <summary>
    /// Du lieu seed: PosterFile la ten file TMDB; Thumb/Poster la URL day du toi CDN.
    /// </summary>
    private sealed record SeedMovieRow(
        string Name,
        string Slug,
        string OriginName,
        int Year,
        double ImdbScore,
        int ViewCount,
        string Status,
        string Quality,
        string Lang,
        string Type,
        int? Duration,
        string Description,
        string PosterFile)
    {
        public string Thumb => TmdbThumb(PosterFile);
        public string Poster => TmdbPoster(PosterFile);
    }
}
