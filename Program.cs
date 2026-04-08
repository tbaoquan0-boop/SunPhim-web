using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using StackExchange.Redis;
using SunPhim.Controllers;
using SunPhim.Data;
using SunPhim.Jobs;
using SunPhim.Services;
using SunPhim.Services.Cache;
using SunPhim.Services.Crawler;
using SunPhim.Services.Streaming;

var builder = WebApplication.CreateBuilder(args);

// CORS - cho phep moi origin trong Development
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

// Cache Service (Redis neu co, fallback Memory Cache neu khong)
builder.Services.AddMemoryCache();
TryRegisterRedisCache(builder.Services, builder.Configuration);

// HTTP Clients
builder.Services.AddHttpClient<IOphimService, OphimService>();
builder.Services.AddHttpClient<ITmdbService, TmdbService>();
builder.Services.AddHttpClient<IProxyService, ProxyService>();
builder.Services.AddHttpClient<IGDriveService, GDriveService>();
builder.Services.AddHttpClient<StreamingController>();

// Streaming Services
builder.Services.AddScoped<IAntiHotlinkService, AntiHotlinkService>();
builder.Services.AddScoped<IGDriveService, GDriveService>();

// Services
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAdService, AdService>();

// Quartz Scheduler
builder.Services.AddQuartz(q =>
{
    q.AddJob<OphimCrawlJob>(opts => opts
        .WithIdentity("ophim-crawl-incremental")
        .StoreDurably()
        .UsingJobData(new JobDataMap { { "JobType", "incremental" }, { "Page", "1" } }));

    q.AddTrigger(opts => opts
        .ForJob("ophim-crawl-incremental")
        .WithIdentity("ophim-crawl-incremental-trigger")
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever()));

    q.AddJob<OphimCrawlJob>(opts => opts
        .WithIdentity("ophim-crawl-full")
        .StoreDurably()
        .UsingJobData(new JobDataMap { { "JobType", "full" } }));

    q.AddTrigger(opts => opts
        .ForJob("ophim-crawl-full")
        .WithIdentity("ophim-crawl-full-trigger")
        .WithCronSchedule("0 0 2 * * ?"));

    q.AddJob<FakeViewJob>(opts => opts
        .WithIdentity("fake-view-job")
        .StoreDurably());

    q.AddTrigger(opts => opts
        .ForJob("fake-view-job")
        .WithIdentity("fake-view-trigger")
        .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever()));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opts =>
    {
        opts.SuppressModelStateInvalidFilter = false;
    });
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

// Global exception handler - bat loi va tra ve JSON
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

app.UseAuthorization();
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
