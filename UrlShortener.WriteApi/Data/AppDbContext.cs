using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;

namespace UrlShortener.WriteApi.Data
{
    /// <summary>
    /// The Entity Framework Core database context.
    /// Configures the schema and the high-performance SQL sequence used for URL generation.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Url> Urls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configures a database-level sequence starting at 1.
            // We no longer need to start at a massive number because Hashids automatically pads 
            // the output to our desired length (minHashLength: 7).
            // This provides extremely fast sequential ID generation without IDENTITY column locking overhead.
            modelBuilder.HasSequence<long>("UrlCounter")
                        .StartsAt(1)
                        .IncrementsBy(1);

            modelBuilder.Entity<Url>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Binds the primary key to the custom SQL sequence.
                entity.Property(e => e.Id)
                      .HasDefaultValueSql("NEXT VALUE FOR UrlCounter");

                // Enforces a unique constraint on the database level.
                // This is critical to prevent race conditions if two concurrent requests attempt to reserve the same Custom Alias.
                entity.HasIndex(e => e.ShortUrl).IsUnique();
                entity.Property(e => e.ShortUrl).IsRequired().HasMaxLength(20);

                entity.Property(e => e.OriginalUrl).IsRequired().HasMaxLength(2048);
            });
        }
    }
}
