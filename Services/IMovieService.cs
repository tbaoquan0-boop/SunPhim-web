using SunPhim.Models;
using SunPhim.Models.DTOs;

namespace SunPhim.Services;

public interface IMovieService
{
    Task<IEnumerable<Movie>> GetAllAsync();
    Task<Movie?> GetByIdAsync(int id);
    Task<Movie?> GetBySlugAsync(string slug);
    Task<MovieDetailDto?> GetDetailBySlugAsync(string slug, string baseUrl);
    Task<IEnumerable<MovieListDto>> GetListDtoAsync(int page = 1, int pageSize = 20);
    Task<IEnumerable<MovieListDto>> GetByTypeAsync(string type, string baseUrl);
    Task<IEnumerable<MovieListDto>> GetByCategoryAsync(string categorySlug, string baseUrl);
    Task<IEnumerable<MovieListDto>> SearchAsync(string keyword, string baseUrl);
    Task<Movie> CreateAsync(Movie movie);
    Task<Movie?> UpdateAsync(int id, Movie movie);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(string externalId);

    // Homepage
    Task<HomePageDto> GetHomePageDataAsync(string baseUrl);
    Task<List<MovieListDto>> GetFeaturedMoviesAsync(string baseUrl, int limit = 10);
    Task<List<MovieListDto>> GetTrendingMoviesAsync(string baseUrl, int limit = 20);
    Task<List<MovieListDto>> GetNewReleasesAsync(string baseUrl, int limit = 20);
    Task<List<MovieListDto>> GetTopRatedMoviesAsync(string baseUrl, int limit = 20);
    Task<List<MovieListDto>> GetRandomFeaturedMoviesAsync(string baseUrl, int count = 5);
    Task<List<CategorySection>> GetMoviesByCategoryAsync(string baseUrl, int limit = 12);
    Task<List<MovieListDto>> GetContinueWatchingAsync(int? userId, string baseUrl, int limit = 10);
}
