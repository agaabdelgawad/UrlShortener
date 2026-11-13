using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using UrlShortener.ReadApi.DTOs;

namespace UrlShortener.ReadApi.Controllers
{
    /// <summary>
    /// Handles the core read operations of the CQRS architecture.
    /// Designed for extreme performance, catching requests at the root level to keep URLs short.
    /// </summary>
    [Route("")]
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly IDatabase _redisDb;
        private readonly string _sqlConnectionString;

        public RedirectController(IConnectionMultiplexer redisMuxer, IConfiguration configuration)
        {
            _redisDb = redisMuxer.GetDatabase();
            _sqlConnectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        /// <summary>
        /// Resolves a short URL using the Cache-Aside pattern.
        /// Checks Redis for a low-latency hit. On a miss, queries the database using Dapper,
        /// validates the logical state (deleted/expired), and updates the cache.
        /// </summary>
        /// <param name="shortUrl">The Base62 string or custom alias.</param>
        /// <returns>
        /// 302 Redirect on success, 404 Not Found if missing/deleted, or 410 Gone if expired.
        /// </returns>
        [HttpGet("{shortUrl}")]
        public async Task<IActionResult> RedirectToOriginal(string shortUrl)
        {
            var cachedOriginalUrl = await _redisDb.StringGetAsync(shortUrl);

            if (!cachedOriginalUrl.IsNullOrEmpty)
            {
                return Redirect(cachedOriginalUrl.ToString());
            }

            using var sqlConnection = new SqlConnection(_sqlConnectionString);
            var query = "SELECT OriginalUrl, ExpirationDate, IsDeleted FROM Urls WHERE ShortUrl = @ShortUrl";
            var url = await sqlConnection.QuerySingleOrDefaultAsync<UrlResponseDto>(query, new { ShortUrl = shortUrl });

            if (url == null || url.IsDeleted)
            {
                return NotFound(new { error = "Url not found." });
            }

            if (url.ExpirationDate.HasValue && url.ExpirationDate.Value < DateTime.UtcNow)
            {
                return StatusCode(StatusCodes.Status410Gone, new { error = "This link has expired." });
            }

            // Dynamically calculates the cache Time-To-Live (TTL).
            // Prevents stale data by ensuring the Redis key expires exactly when the URL expires, 
            // while capping the maximum cache duration to 24 hours to optimize RAM usage.
            TimeSpan cacheExpiry = TimeSpan.FromHours(24);
            if (url.ExpirationDate.HasValue)
            {
                var timeUntilExpiration = url.ExpirationDate.Value - DateTime.UtcNow;
                cacheExpiry = timeUntilExpiration < cacheExpiry ? timeUntilExpiration : cacheExpiry;
            }

            await _redisDb.StringSetAsync(shortUrl, url.OriginalUrl, cacheExpiry);

            return Redirect(url.OriginalUrl);
        }
    }
}
