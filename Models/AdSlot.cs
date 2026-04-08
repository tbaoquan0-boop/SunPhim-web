using System.Text.RegularExpressions;

namespace SunPhim.Models;

public class AdSlot
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string AdCode { get; set; } = string.Empty;
    public string? PageUrlPattern { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Banner> Banners { get; set; } = new List<Banner>();

    public bool IsCurrentlyActive()
    {
        if (!IsActive) return false;
        var now = DateTime.UtcNow;
        if (StartDate.HasValue && now < StartDate.Value) return false;
        if (EndDate.HasValue && now > EndDate.Value) return false;
        return true;
    }

    public bool MatchesPage(string url)
    {
        if (string.IsNullOrWhiteSpace(PageUrlPattern)) return true;
        try
        {
            return Regex.IsMatch(url, PageUrlPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

public class Banner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public string? AltText { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int AdSlotId { get; set; }
    public AdSlot? AdSlot { get; set; }

    public bool IsCurrentlyActive()
    {
        if (!IsActive) return false;
        var now = DateTime.UtcNow;
        if (StartDate.HasValue && now < StartDate.Value) return false;
        if (EndDate.HasValue && now > EndDate.Value) return false;
        return true;
    }
}
