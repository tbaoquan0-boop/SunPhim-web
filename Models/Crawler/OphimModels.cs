namespace SunPhim.Models.Crawler;

public class OphimListResponse
{
    public bool Status { get; set; }
    public List<OphimMovieItem> Items { get; set; } = new();
    public string PathImage { get; set; } = "https://img.ophim.live/uploads/movies/";
    public OphimPagination Pagination { get; set; } = new();
}

public class OphimMovieItem
{
    public OphimTmdb Tmdb { get; set; } = new();
    public OphimTmdb Imdb { get; set; } = new();
    public OphimModified Modified { get; set; } = new();
    public string Id => MongoId;
    public string MongoId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string OriginName { get; set; } = string.Empty;
    public string ThumbUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class OphimMovieDetail
{
    public bool Status { get; set; }
    public string Msg { get; set; } = string.Empty;
    public OphimMovie Movie { get; set; } = new();
    public List<OphimEpisodeServer> Episodes { get; set; } = new();
}

public class OphimMovie
{
    public OphimTmdb Tmdb { get; set; } = new();
    public OphimTmdb Imdb { get; set; } = new();
    public OphimModified Created { get; set; } = new();
    public OphimModified Modified { get; set; } = new();
    public string Id => MongoId;
    public string MongoId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OriginName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "single";
    public string Status { get; set; } = string.Empty;
    public string ThumbUrl { get; set; } = string.Empty;
    public bool IsCopyright { get; set; }
    public bool SubDocquyen { get; set; }
    public string? TrailerUrl { get; set; }
    public string Time { get; set; } = string.Empty;
    public string EpisodeCurrent { get; set; } = string.Empty;
    public string EpisodeTotal { get; set; } = string.Empty;
    public string Quality { get; set; } = "HD";
    public string Lang { get; set; } = "Vietsub";
    public string Notify { get; set; } = string.Empty;
    public string Showtimes { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int Year { get; set; }
    public int View { get; set; }
    public List<string> Actor { get; set; } = new();
    public List<string> Director { get; set; } = new();
    public List<OphimCategory> Category { get; set; } = new();
    public List<OphimCategory> Country { get; set; } = new();
    public bool Chieurap { get; set; }
    public string PosterUrl { get; set; } = string.Empty;
    public List<string> AlternativeNames { get; set; } = new();
    public List<string> LangKey { get; set; } = new();
}

public class OphimTmdb
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public int? Season { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
}

public class OphimModified
{
    public string Time { get; set; } = string.Empty;
}

public class OphimPagination
{
    public int TotalItems { get; set; }
    public int TotalItemsPerPage { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class OphimCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class OphimEpisodeServer
{
    public string ServerName { get; set; } = string.Empty;
    public bool IsAi { get; set; }
    public List<OphimEpisodeItem> ServerData { get; set; } = new();
}

public class OphimEpisodeItem
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string LinkEmbed { get; set; } = string.Empty;
    public string LinkM3u8 { get; set; } = string.Empty;
}
