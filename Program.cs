using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using SunPhim.Controllers;
using SunPhim.Data;
using SunPhim.Jobs;
using SunPhim.Services;
using SunPhim.Services.Cache;
using SunPhim.Services.Crawler;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Response Cache
builder.Services.AddResponseCaching();

// Cache Service (Redis nếu có, fallback Memory Cache nếu không)
builder.Services.AddMemoryCache();
TryRegisterRedisCache(builder.Services, builder.Configuration);

// HTTP Clients
builder.Services.AddHttpClient<INguonCService, NguonCService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5
    });
builder.Services.AddHttpClient<StreamingController>();

// Core Services
builder.Services.AddScoped<MovieService>();
builder.Services.AddScoped<EpisodeService>();
builder.Services.AddScoped<CategoryService>();

// Crawler Background Service (native .NET PeriodicTimer)
builder.Services.AddHostedService<NguonCCrawlerBackgroundService>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthorization();
app.UseResponseCaching();

// Global exception handler
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "Lỗi server: " + ex.Message,
            detail = app.Environment.IsDevelopment() ? ex.ToString() : null
        });
    }
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=3600");
    }
});

app.MapControllers();

app.MapFallbackToFile("index.html", new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
    }
});

app.Run();

static void TryRegisterRedisCache(IServiceCollection services, IConfiguration config)
{
    var redisConn = config["Redis:ConnectionString"];
    if (string.IsNullOrWhiteSpace(redisConn))
    {
        services.AddSingleton<ICacheService, MemoryCacheService>();
        return;
    }

    try
    {
        var options = ConfigurationOptions.Parse(redisConn);
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 5000;
        var redis = ConnectionMultiplexer.Connect(options);

        if (redis.IsConnected)
        {
            services.AddSingleton<IConnectionMultiplexer>(redis);
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }
    }
    catch
    {
        services.AddSingleton<ICacheService, MemoryCacheService>();
    }
}
