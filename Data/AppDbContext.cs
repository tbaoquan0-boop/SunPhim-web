using Microsoft.EntityFrameworkCore;
using SunPhim.Models;

namespace SunPhim.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MovieCategory> MovieCategories => Set<MovieCategory>();
    public DbSet<WatchHistory> WatchHistories => Set<WatchHistory>();
    public DbSet<AdSlot> AdSlots => Set<AdSlot>();
    public DbSet<Banner> Banners => Set<Banner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Movie
        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.ExternalId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Year);
        });

        // Episode - quan hệ 1-N với Movie
        modelBuilder.Entity<Episode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Movie)
                .WithMany(m => m.Episodes)
                .HasForeignKey(e => e.MovieId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.MovieId, e.EpisodeNumber }).IsUnique();
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // MovieCategory - bảng trung gian N-N
        modelBuilder.Entity<MovieCategory>(entity =>
        {
            entity.HasKey(e => new { e.MovieId, e.CategoryId });

            entity.HasOne(e => e.Movie)
                .WithMany(m => m.MovieCategories)
                .HasForeignKey(e => e.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.MovieCategories)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WatchHistory
        modelBuilder.Entity<WatchHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Movie)
                .WithMany()
                .HasForeignKey(e => e.MovieId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Episode)
                .WithMany()
                .HasForeignKey(e => e.EpisodeId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.WatchedAt);
            entity.HasIndex(e => e.UserId);
        });

        // AdSlot
        modelBuilder.Entity<AdSlot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Position).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Position);
            entity.HasIndex(e => e.IsActive);
        });

        // Banner
        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(2000);
            entity.HasOne(e => e.AdSlot)
                .WithMany(s => s.Banners)
                .HasForeignKey(e => e.AdSlotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.IsActive);
        });
    }
}
