using Microsoft.AspNetCore.Mvc;
using SunPhim.Services;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Models.Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name) || string.IsNullOrWhiteSpace(category.Slug))
            return BadRequest(new { message = "Tên và Slug là bắt buộc" });

        var created = await _categoryService.CreateAsync(category);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Models.Category category)
    {
        var updated = await _categoryService.UpdateAsync(id, category);
        if (updated == null) return NotFound(new { message = "Không tìm thấy thể loại" });
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _categoryService.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "Không tìm thấy thể loại" });
        return NoContent();
    }
}
