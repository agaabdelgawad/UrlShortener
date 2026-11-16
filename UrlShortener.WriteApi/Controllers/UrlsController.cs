using HashidsNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Utils;
using UrlShortener.WriteApi.Data;
using UrlShortener.WriteApi.DTOs;
using UrlShortener.WriteApi.Settings;

namespace UrlShortener.WriteApi.Controllers
{
    /// <summary>
    /// Handles the "Command" side of the CQRS architecture.
    /// Responsible for state mutations: creating, updating, and soft-deleting URLs.
    /// Uses Entity Framework Core for transactional integrity and interacts with Redis 
    /// strictly for Cache Invalidation to keep the Read API synchronized.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UrlsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDatabase _redisDb;
        private readonly UrlShortenerSettings _settings;

        public UrlsController(AppDbContext context, IConnectionMultiplexer redisMuxer, IOptions<UrlShortenerSettings> options)
        {
            _context = context;
            _settings = options.Value;
            _redisDb = redisMuxer.GetDatabase();
        }

        /// <summary>
        /// Creates a new shortened URL or registers a custom alias.
        /// Uses Hashids for ID Obfuscation to prevent URL predictable scraping.
        /// Implements Write-Through Caching to Redis to handle eventual consistency.
        /// </summary>
        /// <param name="request">The DTO containing the original URL, optional custom alias, and optional expiration date.</param>
        [HttpPost]
        public async Task<IActionResult> ShortenUrl([FromBody] ShortenUrlRequestDto request)
        {
            long urlId = 0;
            string generatedShortUrl = string.Empty;

            // Scenario 1: User provided a Custom Alias
            if (!string.IsNullOrWhiteSpace(request.CustomAlias))
            {
                var aliasExists = await _context.Urls.AnyAsync(u => u.ShortUrl == request.CustomAlias);
                if (aliasExists)
                {
                    return Conflict(new { error = $"The custom alias '{request.CustomAlias}' is already taken." });
                }

                urlId = await GetNextSequenceValueAsync();
                generatedShortUrl = request.CustomAlias;
            }
            // Scenario 2: Auto-generate Short URL using Hashids (ID Obfuscation)
            else
            {
                bool isUnique = false;
                int maxRetries = 5;
                int attempts = 0;

                // Obfuscation: Replace Base62 with Hashids
                var hashids = new Hashids(_settings.HashidsSalt, minHashLength: 7);

                while (!isUnique && attempts < maxRetries)
                {
                    urlId = await GetNextSequenceValueAsync();
                    generatedShortUrl = hashids.EncodeLong(urlId);

                    var exists = await _context.Urls.AnyAsync(u => u.ShortUrl == generatedShortUrl);
                    if (!exists)
                    {
                        isUnique = true;
                    }
                    attempts++;
                }

                if (!isUnique)
                {
                    return StatusCode(500, new { error = "Failed to generate a unique short URL. Please try again." });
                }
            }

            var newUrl = new Url
            {
                Id = urlId,
                ShortUrl = generatedShortUrl,
                OriginalUrl = request.OriginalUrl,
                CreationDate = DateTime.UtcNow,
                ExpirationDate = request.ExpirationDate
            };

            _context.Urls.Add(newUrl);
            await _context.SaveChangesAsync();

            // Write-Through Cache: Save to Redis immediately after DB save
            TimeSpan cacheExpiry = TimeSpan.FromHours(24);
            if (request.ExpirationDate.HasValue)
            {
                var timeUntilExpiration = request.ExpirationDate.Value - DateTime.UtcNow;
                cacheExpiry = timeUntilExpiration < cacheExpiry ? timeUntilExpiration : cacheExpiry;
            }

            await _redisDb.StringSetAsync(generatedShortUrl, request.OriginalUrl, cacheExpiry);

            var finalUrl = $"{_settings.BaseUrl}{generatedShortUrl}";

            return Ok(new
            {
                shortUrl = finalUrl,
                originalUrl = request.OriginalUrl,
                expirationDate = request.ExpirationDate
            });
        }

        /// <summary>
        /// Manually fetches the next value from the high-performance SQL Server sequence.
        /// Architecturally required because EF Core natively assigns sequence values during SaveChanges, 
        /// but the system design dictates that the ID must be known upfront to generate the Base62 ShortUrl.
        /// </summary>
        private async Task<long> GetNextSequenceValueAsync()
        {
            long nextValue = 0;

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT NEXT VALUE FOR UrlCounter";

                var result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    throw new InvalidOperationException("Critical Error: Database sequence returned null.");
                }

                nextValue = Convert.ToInt64(result);

                if (nextValue <= 0)
                {
                    throw new InvalidOperationException($"Critical Error: Invalid sequence value generated from database: {nextValue}");
                }
            }
            finally
            {
                await connection.CloseAsync();
            }

            return nextValue;
        }

        /// <summary>
        /// Soft-deletes a URL to preserve historical data and analytics.
        /// Architecturally critical: Immediately invalidates the Redis cache (Cache Invalidation) to ensure the Read API 
        /// stops redirecting traffic to this logically deleted link on the very next request.
        /// </summary>
        [HttpDelete("{shortUrl}")]
        public async Task<IActionResult> DeleteUrl(string shortUrl)
        {
            var url = await _context.Urls.FirstOrDefaultAsync(u => u.ShortUrl == shortUrl);

            if (url == null || url.IsDeleted)
            {
                return NotFound("Url not found or already deleted.");
            }

            url.IsDeleted = true; // Soft Delete
            await _context.SaveChangesAsync();

            await _redisDb.KeyDeleteAsync(shortUrl);

            return Ok("Url deleted successfully.");
        }

        /// <summary>
        /// Updates the expiration date of an existing URL.
        /// Triggers Cache Invalidation to force the Read API to fall back to the database, 
        /// recalculate the new TTL, and prevent stale expiration states from surviving in the cache.
        /// </summary>
        [HttpPatch("{shortUrl}/expiration")]
        public async Task<IActionResult> UpdateExpiration(string shortUrl, [FromBody] UpdateExpirationDto dto)
        {
            var url = await _context.Urls.FirstOrDefaultAsync(u => u.ShortUrl == shortUrl);

            if (url == null || url.IsDeleted)
            {
                return NotFound("Url not found.");
            }

            url.ExpirationDate = dto.NewExpirationDate;
            await _context.SaveChangesAsync();


            await _redisDb.KeyDeleteAsync(shortUrl);

            return Ok("Expiration date updated successfully.");
        }
    }
}