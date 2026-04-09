using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunPhim.Models;
using SunPhim.Services;

namespace SunPhim.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : null;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { success = false, message = "Email và mật khẩu không được để trống." });

        var (user, token) = await _userService.ValidateAndLoginAsync(req.Email, req.Password);
        if (user == null || token == null)
            return Unauthorized(new { success = false, message = "Email hoặc mật khẩu không đúng." });

        return Ok(new AuthResponse
        {
            User = ToUserDto(user),
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(30).ToString("o"),
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });

        if (req.Password.Length < 8)
            return BadRequest(new { success = false, message = "Mật khẩu phải có ít nhất 8 ký tự." });

        try
        {
            var user = await _userService.RegisterAsync(req.Username, req.Email, req.Password);
            var (_, token) = await _userService.ValidateAndLoginAsync(req.Email, req.Password);
            return Ok(new AuthResponse
            {
                User = ToUserDto(user),
                Token = token!,
                ExpiresAt = DateTime.UtcNow.AddDays(30).ToString("o"),
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { success = false, message = "Chưa đăng nhập." });

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null) return NotFound(new { success = false, message = "Không tìm thấy người dùng." });

        return Ok(new { user = ToUserDto(user) });
    }

    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { success = false, message = "Chưa đăng nhập." });

        var history = await _userService.GetWatchHistoryAsync(userId.Value);
        return Ok(history.Select(h => new WatchHistoryDto
        {
            MovieId = h.MovieId,
            MovieName = h.Movie?.Name ?? "",
            MovieSlug = h.Movie?.Slug ?? "",
            MoviePoster = h.Movie?.Poster ?? h.Movie?.Thumb ?? "",
            EpisodeNumber = h.Episode?.EpisodeNumber ?? 1,
            WatchedAt = h.WatchedAt.ToString("o"),
        }).ToList());
    }

    [Authorize]
    [HttpPost("history")]
    public async Task<IActionResult> AddHistory([FromBody] AddHistoryRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { success = false, message = "Chưa đăng nhập." });

        if (req.MovieId <= 0 || req.EpisodeId <= 0)
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

        await _userService.AddWatchHistoryAsync(userId.Value, req.MovieId, req.EpisodeId);
        return Ok(new { success = true, message = "Đã ghi nhận lịch sử xem." });
    }

    [Authorize]
    [HttpPost("rate")]
    public async Task<IActionResult> RateMovie([FromBody] RateMovieRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { success = false, message = "Chưa đăng nhập." });

        var ok = await _userService.RateMovieAsync(userId.Value, req.MovieId, req.Score);
        if (!ok) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

        return Ok(new { success = true, message = "Đã lưu đánh giá." });
    }

    private static object ToUserDto(User u) => new
    {
        u.Id,
        u.Username,
        u.Email,
        u.AvatarUrl,
        CreatedAt = u.CreatedAt.ToString("o"),
    };
}

// DTOs
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public object User { get; set; } = new { };
    public string Token { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
}

public class AddHistoryRequest
{
    public int MovieId { get; set; }
    public int EpisodeId { get; set; }
}

public class RateMovieRequest
{
    public int MovieId { get; set; }
    public int Score { get; set; }
}

public class WatchHistoryDto
{
    public int MovieId { get; set; }
    public string MovieName { get; set; } = string.Empty;
    public string MovieSlug { get; set; } = string.Empty;
    public string MoviePoster { get; set; } = string.Empty;
    public int EpisodeNumber { get; set; }
    public string WatchedAt { get; set; } = string.Empty;
}
