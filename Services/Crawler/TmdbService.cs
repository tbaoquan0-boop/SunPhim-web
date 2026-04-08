namespace SunPhim.Services.Crawler;

public interface ITmdbService
{
    Task<TmdbEnrichment?> GetMovieEnrichmentAsync(string imdbId, CancellationToken ct = default);
    Task<TmdbSearchResult?> SearchMovieAsync(string query, CancellationToken ct = default);
}

public class TmdbEnrichment
{
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<TmdbCast> Cast { get; set; } = new();
    public string? Overview { get; set; }
}

public class TmdbSearchResult
{
    public int TmdbId { get; set; }
    public string? PosterPath { get; set; }
    public double VoteAverage { get; set; }
    public string? ImdbId { get; set; }
}

public class TmdbCast
{
    public string Name { get; set; } = string.Empty;
    public string Character { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class TmdbService : ITmdbService
{
    private readonly HttpClient _http;
    private readonly ILogger<TmdbService> _log;
    private readonly string? _apiKey;
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private const string ImgBase = "https://image.tmdb.org/t/p/";

    public TmdbService(HttpClient http, IConfiguration config, ILogger<TmdbService> log)
    {
        _http = http;
        _log = log;
        _apiKey = config["TMDB:ApiKey"];
    }

    public async Task<TmdbEnrichment?> GetMovieEnrichmentAsync(string imdbId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _log.LogWarning("TMDB API key not configured. Set TMDB:ApiKey in appsettings.json");
            return null;
        }

        if (string.IsNullOrWhiteSpace(imdbId) || !imdbId.StartsWith("tt"))
        {
            _log.LogWarning("Invalid IMDB ID: {Id}", imdbId);
            return null;
        }

        try
        {
            var url = $"{BaseUrl}/find/{imdbId}?api_key={_apiKey}&external_source=imdb_id";
            using var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var movieResults = doc.RootElement.GetProperty("movie_results");
            if (movieResults.GetArrayLength() == 0) return null;

            var movie = movieResults[0];
            var tmdbId = movie.GetProperty("id").GetInt32();

            var detail = await GetDetailAsync(tmdbId, ct);
            return detail;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error fetching TMDB enrichment for {ImdbId}", imdbId);
            return null;
        }
    }

    public async Task<TmdbSearchResult?> SearchMovieAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        try
        {
            var encoded = Uri.EscapeDataString(query);
            var url = $"{BaseUrl}/search/movie?api_key={_apiKey}&query={encoded}";
            using var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0) return null;

            var first = results[0];
            return new TmdbSearchResult
            {
                TmdbId = first.GetProperty("id").GetInt32(),
                PosterPath = first.TryGetProperty("poster_path", out var p) ? $"{ImgBase}w500{p.GetString()}" : null,
                VoteAverage = first.GetProperty("vote_average").GetDouble(),
                ImdbId = null
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error searching TMDB for {Query}", query);
            return null;
        }
    }

    private async Task<TmdbEnrichment?> GetDetailAsync(int tmdbId, CancellationToken ct)
    {
        try
        {
            var url = $"{BaseUrl}/movie/{tmdbId}?api_key={_apiKey}&append_to_response=credits";
            using var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            var root = doc.RootElement;
            var credits = root.GetProperty("credits");
            var castArr = credits.GetProperty("cast");

            var genres = new List<string>();
            foreach (var g in root.GetProperty("genres").EnumerateArray())
            {
                genres.Add(g.GetProperty("name").GetString() ?? "");
            }

            var cast = new List<TmdbCast>();
            foreach (var c in castArr.EnumerateArray().Take(10))
            {
                cast.Add(new TmdbCast
                {
                    Name = c.GetProperty("name").GetString() ?? "",
                    Character = c.GetProperty("character").GetString() ?? "",
                    Order = c.GetProperty("order").GetInt32()
                });
            }

            return new TmdbEnrichment
            {
                VoteAverage = root.GetProperty("vote_average").GetDouble(),
                VoteCount = root.GetProperty("vote_count").GetInt32(),
                PosterPath = root.TryGetProperty("poster_path", out var pp) ? $"{ImgBase}w500{pp.GetString()}" : null,
                BackdropPath = root.TryGetProperty("backdrop_path", out var bp) ? $"{ImgBase}w1280{bp.GetString()}" : null,
                Genres = genres,
                Cast = cast,
                Overview = root.TryGetProperty("overview", out var ov) ? ov.GetString() : null
            };
        }
        catch
        {
            return null;
        }
    }
}
