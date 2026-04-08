using Microsoft.EntityFrameworkCore;
using SunPhim.Data;
using SunPhim.Models;

namespace SunPhim.Services;

public class EpisodeService : IEpisodeService
{
    private readonly AppDbContext _context;

    public EpisodeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Episode>> GetByMovieIdAsync(int movieId)
    {
        return await _context.Episodes
            .AsNoTracking()
            .Where(e => e.MovieId == movieId && e.Status == "active")
            .OrderBy(e => e.EpisodeNumber)
            .ToListAsync();
    }

    public async Task<Episode?> GetByIdAsync(int id)
    {
        return await _context.Episodes
            .AsNoTracking()
            .Include(e => e.Movie)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Episode?> GetByMovieAndNumberAsync(int movieId, int episodeNumber)
    {
        return await _context.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.MovieId == movieId && e.EpisodeNumber == episodeNumber);
    }

    public async Task<Episode> CreateAsync(Episode episode)
    {
        episode.CreatedAt = DateTime.UtcNow;
        episode.UpdatedAt = DateTime.UtcNow;
        _context.Episodes.Add(episode);
        await _context.SaveChangesAsync();
        return episode;
    }

    public async Task<Episode?> UpdateAsync(int id, Episode episode)
    {
        var existing = await _context.Episodes.FindAsync(id);
        if (existing == null) return null;

        existing.Name = episode.Name;
        existing.EpisodeNumber = episode.EpisodeNumber;
        existing.EmbedLink = episode.EmbedLink;
        existing.FileUrl = episode.FileUrl;
        existing.Server = episode.Server;
        existing.Status = episode.Status;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var episode = await _context.Episodes.FindAsync(id);
        if (episode == null) return false;

        _context.Episodes.Remove(episode);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task IncrementViewAsync(int episodeId)
    {
        var episode = await _context.Episodes
            .Include(e => e.Movie)
            .FirstOrDefaultAsync(e => e.Id == episodeId);

        if (episode?.Movie != null)
        {
            episode.Movie.ViewCount++;
            await _context.SaveChangesAsync();
        }
    }
}
