using SunPhim.Models;

namespace SunPhim.Services;

public interface IEpisodeService
{
    Task<IEnumerable<Episode>> GetByMovieIdAsync(int movieId);
    Task<Episode?> GetByIdAsync(int id);
    Task<Episode?> GetByMovieAndNumberAsync(int movieId, int episodeNumber);
    Task<Episode> CreateAsync(Episode episode);
    Task<Episode?> UpdateAsync(int id, Episode episode);
    Task<bool> DeleteAsync(int id);
    Task IncrementViewAsync(int episodeId);
}
