using Microsoft.EntityFrameworkCore;
using SunPhim.Data;
using SunPhim.Models;

namespace SunPhim.Services;

public class EpisodeService
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
