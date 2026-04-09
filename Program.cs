using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using StackExchange.Redis;
using SunPhim.Controllers;
using SunPhim.Data;
using SunPhim.Jobs;
using SunPhim.Services;
using SunPhim.Services.Cache;
using SunPhim.Services.Crawler;
using SunPhim.Services.Streaming;
using System.Text;

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

// Response Cache cho OutputCache attribute
builder.Services.AddResponseCaching();

// Cache Service (Redis neu co, fallback Memory Cache neu khong)
builder.Services.AddMemoryCache();
TryRegisterRedisCache(builder.Services, builder.Configuration);

// HTTP Clients (crawl chi NguonC + User-Agent Chrome trong NguonCService)
builder.Services.AddHttpClient<INguonCService, NguonCService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5
    });
builder.Services.AddHttpClient<ITmdbService, TmdbService>();
builder.Services.AddHttpClient<IProxyService, ProxyService>();
builder.Services.AddHttpClient<IGDriveService, GDriveService>();
builder.Services.AddHttpClient<StreamingController>();
builder.Services.AddHttpClient<ImageController>();

// Streaming Services
builder.Services.AddScoped<IAntiHotlinkService, AntiHotlinkService>();
builder.Services.AddScoped<IGDriveService, GDriveService>();

// Services
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAdService, AdService>();
builder.Services.AddScoped<IUserService, UserService>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SunPhim_SuperSecretKey_MustBeAtLeast32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SunPhim";
builder.Services.AddAuthentication()
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

// Quartz Scheduler
builder.Services.AddQuartz(q =>
{
    // Full crawl - chay 1 lan/tuan (Chu nhat 3h sang)
    q.AddJob<NguonCCrawlJob>(opts => opts
        .WithIdentity("nguonc-crawl-full")
        .StoreDurably()
        .UsingJobData(new JobDataMap { { "JobType", "full" } }));

    q.AddTrigger(opts => opts
        .ForJob("nguonc-crawl-full")
        .WithIdentity("nguonc-crawl-full-trigger")
        .WithCronSchedule("0 0 3 ? * SUN"));

    // Incremental crawl - phim moi cap nhat: moi gio luc phut 5 (1:05, 2:05 ... 23:05) — gio Viet Nam
    q.AddJob<NguonCCrawlJob>(opts => opts
        .WithIdentity("nguonc-crawl-incremental")
        .StoreDurably()
        .UsingJobData(new JobDataMap { { "JobType", "incremental" }, { "Page", "1" } }));

    var vietnamTz = ResolveVietnamTimeZone();
    q.AddTrigger(opts => opts
        .ForJob("nguonc-crawl-incremental")
        .WithIdentity("nguonc-crawl-incremental-trigger")
        .WithCronSchedule("0 5 * * * ?", x => x
            .InTimeZone(vietnamTz)
            .WithMisfireHandlingInstructionFireAndProceed()));

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
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    })
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
app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCaching();

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

// Serve Next.js SPA from wwwroot/frontend/ (nếu có)
var frontendPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "frontend");
if (Directory.Exists(frontendPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath),
        RequestPath = "",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=86400");
        }
    });
}

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

/// <summary>Windows: SE Asia Standard Time; Linux: Asia/Ho_Chi_Minh. Fallback: may gio he thong.</summary>
static TimeZoneInfo ResolveVietnamTimeZone()
{
    foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException) { /* thu id tiep theo */ }
        catch (InvalidTimeZoneException) { /* thu id tiep theo */ }
    }

    return TimeZoneInfo.Local;
}

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
