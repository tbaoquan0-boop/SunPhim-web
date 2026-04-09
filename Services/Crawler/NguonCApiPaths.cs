namespace SunPhim.Services.Crawler;

internal static class NguonCApiPaths
{
    private const string BaseUrl = "https://phim.nguonc.com";

    /// <summary>phim-moi-cap-nhat</summary>
    internal const string PhimMoiCapNhat = "phim-moi-cap-nhat";

    /// <summary>danh-sach/{slug}</summary>
    internal static string DanhSach(string listSlug) =>
        $"danh-sach/{Uri.EscapeDataString(listSlug.Trim())}";

    /// <summary>nam-phat-hanh/{year}</summary>
    internal static string NamPhatHanh(int year) =>
        $"nam-phat-hanh/{Uri.EscapeDataString(year.ToString())}";

    /// <summary>the-loai/{slug}</summary>
    internal static string TheLoai(string slug) =>
        $"the-loai/{Uri.EscapeDataString(slug.Trim())}";

    /// <summary>quoc-gia/{slug}</summary>
    internal static string QuocGia(string slug) =>
        $"quoc-gia/{Uri.EscapeDataString(slug.Trim())}";

    /// <summary>search (keyword lam query string rieng)</summary>
    internal const string Search = "search";

    /// <summary>film/{slug}</summary>
    internal static string FilmDetail(string movieSlug) =>
        $"film/{Uri.EscapeDataString(movieSlug.Trim())}";

    /// <summary>https://phim.nguonc.com/api/films/{endpoint}?page={page}[&extra]</summary>
    internal static string BuildListUrl(string endpoint, int page, string? extraQuery)
    {
        var ep = endpoint.TrimStart('/');
        if (string.IsNullOrEmpty(extraQuery))
            return $"{BaseUrl}/api/films/{ep}?page={page}";
        return $"{BaseUrl}/api/films/{ep}?{extraQuery}&page={page}";
    }

    /// <summary>https://phim.nguonc.com/api/film/{slug}</summary>
    internal static string BuildFilmDetailUrl(string movieSlug) =>
        $"{BaseUrl}/api/film/{Uri.EscapeDataString(movieSlug.Trim())}";
}
