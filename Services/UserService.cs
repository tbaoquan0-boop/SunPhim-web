using Microsoft.EntityFrameworkCore;
using SunPhim.Data;
using SunPhim.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SunPhim.Services;

public interface IUserService
{
    Task<(User? user, string? token)> ValidateAndLoginAsync(string email, string password);
    Task<User> RegisterAsync(string username, string email, string password);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task AddWatchHistoryAsync(int userId, int movieId, int episodeId);
    Task<List<WatchHistory>> GetWatchHistoryAsync(int userId);
    Task<bool> RateMovieAsync(int userId, int movieId, int score);
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;

    public UserService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _jwtSecret = config["Jwt:Secret"] ?? "SunPhim_SuperSecretKey_MustBeAtLeast32Chars!";
        _jwtIssuer = config["Jwt:Issuer"] ?? "SunPhim";
    }

    public async Task<(User? user, string? token)> ValidateAndLoginAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return (null, null);

        if (!VerifyPassword(password, user.PasswordHash))
            return (null, null);

        var token = GenerateJwt(user);
        return (user, token);
    }

    public async Task<User> RegisterAsync(string username, string email, string password)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email đã được sử dụng.");

        if (await _db.Users.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException("Tên người dùng đã được sử dụng.");

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetByIdAsync(int id)
        => await _db.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddWatchHistoryAsync(int userId, int movieId, int episodeId)
    {
        var existing = await _db.WatchHistories
            .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId && w.EpisodeId == episodeId);

        if (existing != null)
        {
            existing.WatchedAt = DateTime.UtcNow;
        }
        else
        {
            _db.WatchHistories.Add(new WatchHistory
            {
                UserId = userId,
                MovieId = movieId,
                EpisodeId = episodeId,
                WatchedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<WatchHistory>> GetWatchHistoryAsync(int userId)
        => await _db.WatchHistories
            .Include(w => w.Movie)
            .Include(w => w.Episode)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.WatchedAt)
            .Take(50)
            .ToListAsync();

    public async Task<bool> RateMovieAsync(int userId, int movieId, int score)
    {
        if (score < 1 || score > 10) return false;
        var movie = await _db.Movies.FindAsync(movieId);
        if (movie == null) return false;

        var existing = await _db.UserRatings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);

        if (existing != null)
        {
            var oldScore = existing.Score;
            existing.Score = score;
            var delta = score - oldScore;
            movie.RatingCount++;
            movie.Rating = Math.Round(((movie.Rating ?? 0) * (movie.RatingCount - 1) + delta) / movie.RatingCount, 1);
        }
        else
        {
            _db.UserRatings.Add(new UserRating
            {
                UserId = userId,
                MovieId = movieId,
                Score = score,
            });
            movie.RatingCount++;
            var current = movie.Rating ?? 0;
            movie.Rating = Math.Round((current * (movie.RatingCount - 1) + score) / movie.RatingCount, 1);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    // ---------- Password hashing ----------
    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + "SunPhim_Salt_v1");
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string hash)
        => HashPassword(password) == hash;

    // ---------- JWT ----------
    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
