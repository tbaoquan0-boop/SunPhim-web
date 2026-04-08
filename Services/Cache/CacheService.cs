using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace SunPhim.Services.Cache;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default);
    bool IsAvailable { get; }
}

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _log;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);

    public bool IsAvailable => true;

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> log)
    {
        _cache = cache;
        _log = log;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue(key, out var value);
        return Task.FromResult(value is T typed ? typed : default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
        };
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached != null) return cached;

        var value = await factory();
        await SetAsync(key, value, expiry ?? DefaultExpiry, ct);
        return value;
    }
}

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IMemoryCache _fallback;
    private readonly ILogger<RedisCacheService> _log;
    private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(5);

    public bool IsAvailable => _redis?.IsConnected ?? false;

    public RedisCacheService(
        IConnectionMultiplexer? redis,
        IMemoryCache fallback,
        ILogger<RedisCacheService> log)
    {
        _redis = redis;
        _fallback = fallback;
        _log = log;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (!IsAvailable) return GetFallback<T>(key);

        try
        {
            var db = _redis!.GetDatabase();
            var json = await db.StringGetAsync(key);
            if (json.IsNullOrEmpty) return default(T?);
            return JsonSerializer.Deserialize<T>(json!.ToString());
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Redis get failed for key {Key}, falling back to memory", key);
            return GetFallback<T>(key);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            SetFallback(key, value, expiry);
            return;
        }

        try
        {
            var db = _redis!.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, expiry ?? _defaultExpiry);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Redis set failed for key {Key}", key);
            SetFallback(key, value, expiry);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _fallback.Remove(key);
            return;
        }

        try
        {
            var db = _redis!.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Redis remove failed for key {Key}", key);
            _fallback.Remove(key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached != null) return cached;

        var value = await factory();
        await SetAsync(key, value, expiry ?? _defaultExpiry, ct);
        return value;
    }

    private T? GetFallback<T>(string key)
    {
        _fallback.TryGetValue(key, out var value);
        return value is T typed ? typed : default;
    }

    private void SetFallback<T>(string key, T value, TimeSpan? expiry)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? _defaultExpiry
        };
        _fallback.Set(key, value, options);
    }
}