using Microsoft.EntityFrameworkCore;
using SunPhim.Data;
using SunPhim.Models;
using SunPhim.Models.DTOs;

namespace SunPhim.Services;

public interface IAdService
{
    Task<List<AdSlot>> GetActiveSlotsForPageAsync(string url, CancellationToken ct = default);
    Task<Banner?> GetRandomBannerForSlotAsync(int slotId, CancellationToken ct = default);
    Task<List<Banner>> GetAllActiveBannersAsync(CancellationToken ct = default);
    Task<BannerDto?> GetActiveBannerAsync(string position, CancellationToken ct = default);
    Task<AdSlot> CreateSlotAsync(AdSlot slot);
    Task<Banner> CreateBannerAsync(Banner banner);
    Task<AdSlot?> UpdateSlotAsync(int id, AdSlot slot);
    Task<Banner?> UpdateBannerAsync(int id, Banner banner);
    Task<bool> DeleteSlotAsync(int id);
    Task<bool> DeleteBannerAsync(int id);
}

public class AdService : IAdService
{
    private readonly AppDbContext _ctx;
    private readonly ILogger<AdService> _log;
    private static readonly Random Rng = new();

    public AdService(AppDbContext ctx, ILogger<AdService> log)
    {
        _ctx = ctx;
        _log = log;
    }

    public async Task<List<AdSlot>> GetActiveSlotsForPageAsync(string url, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _ctx.AdSlots
            .AsNoTracking()
            .Where(s => s.IsActive
                && (!s.StartDate.HasValue || s.StartDate <= now)
                && (!s.EndDate.HasValue || s.EndDate >= now)
                && s.MatchesPage(url))
            .ToListAsync(ct);
    }

    public async Task<Banner?> GetRandomBannerForSlotAsync(int slotId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var banners = await _ctx.Banners
            .AsNoTracking()
            .Where(b => b.AdSlotId == slotId
                && b.IsActive
                && (!b.StartDate.HasValue || b.StartDate <= now)
                && (!b.EndDate.HasValue || b.EndDate >= now))
            .OrderByDescending(b => b.Priority)
            .ThenBy(_ => Rng.Next())
            .ToListAsync(ct);

        return banners.FirstOrDefault();
    }

    public async Task<List<Banner>> GetAllActiveBannersAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _ctx.Banners
            .AsNoTracking()
            .Where(b => b.IsActive
                && (!b.StartDate.HasValue || b.StartDate <= now)
                && (!b.EndDate.HasValue || b.EndDate >= now))
            .ToListAsync(ct);
    }

    public async Task<BannerDto?> GetActiveBannerAsync(string position, CancellationToken ct = default)
    {
        var slot = await _ctx.AdSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Position == position && s.IsCurrentlyActive(), ct);

        if (slot == null) return null;

        var banner = await GetRandomBannerForSlotAsync(slot.Id, ct);
        if (banner == null) return null;

        return new BannerDto
        {
            Id = banner.Id,
            ImageUrl = banner.ImageUrl,
            LinkUrl = banner.LinkUrl,
            AltText = banner.AltText,
            Title = slot.Name,
            Subtitle = null
        };
    }

    public async Task<AdSlot> CreateSlotAsync(AdSlot slot)
    {
        slot.CreatedAt = DateTime.UtcNow;
        _ctx.AdSlots.Add(slot);
        await _ctx.SaveChangesAsync();
        return slot;
    }

    public async Task<Banner> CreateBannerAsync(Banner banner)
    {
        banner.CreatedAt = DateTime.UtcNow;
        _ctx.Banners.Add(banner);
        await _ctx.SaveChangesAsync();
        return banner;
    }

    public async Task<AdSlot?> UpdateSlotAsync(int id, AdSlot slot)
    {
        var existing = await _ctx.AdSlots.FindAsync(id);
        if (existing == null) return null;
        existing.Name = slot.Name;
        existing.Position = slot.Position;
        existing.AdCode = slot.AdCode;
        existing.PageUrlPattern = slot.PageUrlPattern;
        existing.IsActive = slot.IsActive;
        existing.StartDate = slot.StartDate;
        existing.EndDate = slot.EndDate;
        await _ctx.SaveChangesAsync();
        return existing;
    }

    public async Task<Banner?> UpdateBannerAsync(int id, Banner banner)
    {
        var existing = await _ctx.Banners.FindAsync(id);
        if (existing == null) return null;
        existing.Name = banner.Name;
        existing.ImageUrl = banner.ImageUrl;
        existing.LinkUrl = banner.LinkUrl;
        existing.AltText = banner.AltText;
        existing.Width = banner.Width;
        existing.Height = banner.Height;
        existing.Priority = banner.Priority;
        existing.IsActive = banner.IsActive;
        existing.StartDate = banner.StartDate;
        existing.EndDate = banner.EndDate;
        await _ctx.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteSlotAsync(int id)
    {
        var slot = await _ctx.AdSlots.FindAsync(id);
        if (slot == null) return false;
        _ctx.AdSlots.Remove(slot);
        await _ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteBannerAsync(int id)
    {
        var banner = await _ctx.Banners.FindAsync(id);
        if (banner == null) return false;
        _ctx.Banners.Remove(banner);
        await _ctx.SaveChangesAsync();
        return true;
    }
}
