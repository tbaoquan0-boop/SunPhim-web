using System.Text.Json.Serialization;

namespace SunPhim.Models.Crawler;

public class NguonCResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("movie")]
    public NguonCMovieDto Movie { get; set; } = new();
}

public class NguonCMovieDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("original_name")]
    public string OriginalName { get; set; } = string.Empty;

    [JsonPropertyName("thumb_url")]
    public string ThumbUrl { get; set; } = string.Empty;

    [JsonPropertyName("poster_url")]
    public string PosterUrl { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;

    [JsonPropertyName("modified")]
    public string Modified { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("total_episodes")]
    public int TotalEpisodes { get; set; }

    [JsonPropertyName("current_episode")]
    public string CurrentEpisode { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("quality")]
    public string Quality { get; set; } = "HD";

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("director")]
    public string Director { get; set; } = string.Empty;

    [JsonPropertyName("casts")]
    public string Casts { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public Dictionary<string, NguonCCategoryGroupDto> Category { get; set; } = new();

    [JsonPropertyName("episodes")]
    public List<NguonCEpisodeServerDto> Episodes { get; set; } = new();
}

public class NguonCCategoryGroupDto
{
    [JsonPropertyName("group")]
    public NguonCCategoryGroupInfo Group { get; set; } = new();

    [JsonPropertyName("list")]
    public List<NguonCCategoryItemDto> List { get; set; } = new();
}

public class NguonCCategoryGroupInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class NguonCCategoryItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class NguonCEpisodeServerDto
{
    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<NguonCEpisodeItemDto> Items { get; set; } = new();
}

public class NguonCEpisodeItemDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("embed")]
    public string Embed { get; set; } = string.Empty;

    [JsonPropertyName("m3u8")]
    public string M3u8 { get; set; } = string.Empty;
}
