using Microsoft.AspNetCore.Mvc;
using SunPhim.Services;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _categoryService.GetAllAsync());
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured()
    {
        return Ok(await _categoryService.GetFeaturedAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null) return NotFound(new { message = "Không tìm thấy thể loại" });
        return Ok(category);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var category = await _categoryService.GetBySlugAsync(slug);
        if (category == null) return NotFound(new { message = "Không tìm thấy thể loại" });
        return Ok(category);
    }
}
