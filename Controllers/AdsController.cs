using Microsoft.AspNetCore.Mvc;
using SunPhim.Services;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdsController : ControllerBase
{
    private readonly IAdService _adService;
    private readonly ILogger<AdsController> _log;

    public AdsController(IAdService adService, ILogger<AdsController> log)
    {
        _adService = adService;
        _log = log;
    }

    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots([FromQuery] string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "URL is required" });

        var slots = await _adService.GetActiveSlotsForPageAsync(url);
        var result = new List<object>();

        foreach (var slot in slots)
        {
            var banner = await _adService.GetRandomBannerForSlotAsync(slot.Id);
            result.Add(new
            {
                slotId = slot.Id,
                slotName = slot.Name,
                position = slot.Position,
                banner = banner != null ? new
                {
                    bannerId = banner.Id,
                    name = banner.Name,
                    imageUrl = banner.ImageUrl,
                    linkUrl = banner.LinkUrl,
                    altText = banner.AltText,
                    width = banner.Width,
                    height = banner.Height
                } : null,
                adCode = slot.AdCode
            });
        }

        return Ok(result);
    }

    [HttpGet("banners")]
    public async Task<IActionResult> GetAllBanners()
        => Ok(await _adService.GetAllActiveBannersAsync());

    [HttpGet("slots/all")]
    public async Task<IActionResult> GetAllSlots()
        => Ok(await _adService.GetActiveSlotsForPageAsync("/"));

    [HttpPost("slots")]
    public async Task<IActionResult> CreateSlot([FromBody] Models.AdSlot slot)
    {
        if (string.IsNullOrWhiteSpace(slot.Name))
            return BadRequest(new { error = "Tên slot là bắt buộc" });

        var created = await _adService.CreateSlotAsync(slot);
        return CreatedAtAction(nameof(GetSlots), new { }, created);
    }

    [HttpPut("slots/{id:int}")]
    public async Task<IActionResult> UpdateSlot(int id, [FromBody] Models.AdSlot slot)
    {
        var updated = await _adService.UpdateSlotAsync(id, slot);
        if (updated == null) return NotFound(new { error = "Không tìm thấy slot" });
        return Ok(updated);
    }

    [HttpDelete("slots/{id:int}")]
    public async Task<IActionResult> DeleteSlot(int id)
    {
        var deleted = await _adService.DeleteSlotAsync(id);
        if (!deleted) return NotFound(new { error = "Không tìm thấy slot" });
        return NoContent();
    }

    [HttpPost("banners")]
    public async Task<IActionResult> CreateBanner([FromBody] Models.Banner banner)
    {
        if (banner.AdSlotId <= 0)
            return BadRequest(new { error = "AdSlotId không hợp lệ" });

        var created = await _adService.CreateBannerAsync(banner);
        return CreatedAtAction(nameof(GetAllBanners), new { }, created);
    }

    [HttpPut("banners/{id:int}")]
    public async Task<IActionResult> UpdateBanner(int id, [FromBody] Models.Banner banner)
    {
        var updated = await _adService.UpdateBannerAsync(id, banner);
        if (updated == null) return NotFound(new { error = "Không tìm thấy banner" });
        return Ok(updated);
    }

    [HttpDelete("banners/{id:int}")]
    public async Task<IActionResult> DeleteBanner(int id)
    {
        var deleted = await _adService.DeleteBannerAsync(id);
        if (!deleted) return NotFound(new { error = "Không tìm thấy banner" });
        return NoContent();
    }
}
