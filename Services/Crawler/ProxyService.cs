namespace SunPhim.Services.Crawler;

public interface IProxyService
{
    string? GetNextProxy();
    Task<string?> GetRandomProxyAsync(CancellationToken ct = default);
    int TotalProxies { get; }
}

public class ProxyService : IProxyService
{
    private readonly List<string> _proxies;
    private readonly HttpClient _http;
    private readonly ILogger<ProxyService> _log;
    private int _index = 0;
    private readonly object _lock = new();
    private readonly Random _rng = new();

    public int TotalProxies => _proxies.Count;

    public ProxyService(IConfiguration config, HttpClient http, ILogger<ProxyService> log)
    {
        _http = http;
        _log = log;
        _proxies = config.GetSection("Crawler:Proxies")
            .Get<List<string>>() ?? new();
    }

    public string? GetNextProxy()
    {
        if (_proxies.Count == 0) return null;
        lock (_lock)
        {
            var proxy = _proxies[_index % _proxies.Count];
            _index++;
            return proxy;
        }
    }

    public async Task<string?> GetRandomProxyAsync(CancellationToken ct = default)
    {
        if (_proxies.Count == 0) return null;
        var proxy = _proxies[_rng.Next(_proxies.Count)];
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var res = await _http.GetAsync("https://httpbin.org/ip", cts.Token);
            if (res.IsSuccessStatusCode)
            {
                _log.LogDebug("Proxy {Proxy} is alive", proxy);
                return proxy;
            }
        }
        catch
        {
            _proxies.Remove(proxy);
            _log.LogWarning("Dead proxy removed: {Proxy}, remaining: {Count}", proxy, _proxies.Count);
        }
        return GetNextProxy();
    }
}
