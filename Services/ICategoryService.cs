using SunPhim.Models;

namespace SunPhim.Services;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<IEnumerable<Category>> GetFeaturedAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<Category?> GetBySlugAsync(string slug);
    Task<Category> CreateAsync(Category category);
    Task<Category?> UpdateAsync(int id, Category category);
    Task<bool> DeleteAsync(int id);
}
