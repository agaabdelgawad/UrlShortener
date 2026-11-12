using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;

namespace UrlShortener.WriteApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Url> Urls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasSequence<long>("UrlCounter")
                        .StartsAt(100_000_000_000)
                        .IncrementsBy(1);

            modelBuilder.Entity<Url>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                      .HasDefaultValueSql("NEXT VALUE FOR UrlCounter");

                entity.HasIndex(e => e.ShortUrl).IsUnique();
                entity.Property(e => e.ShortUrl).IsRequired().HasMaxLength(20);

                entity.Property(e => e.OriginalUrl).IsRequired().HasMaxLength(2048);
            });
        }
    }
}
