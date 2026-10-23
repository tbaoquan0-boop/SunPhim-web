using Microsoft.EntityFrameworkCore;
using SunPhim.Models;

namespace SunPhim.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            return;
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
    }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MovieCategory> MovieCategories => Set<MovieCategory>();

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
            // Index cho truy vấn trang chủ
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ImdbScore);
            entity.HasIndex(e => e.Rating);
            // Index phức hợp cho sắp xếp theo nhiều tiêu chí
            entity.HasIndex(e => new { e.IsPublished, e.UpdatedAt });
            entity.HasIndex(e => new { e.IsPublished, e.Rating });
            entity.HasIndex(e => new { e.IsPublished, e.ImdbScore });
            entity.HasIndex(e => new { e.Type, e.IsPublished, e.UpdatedAt });
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
    }
}
