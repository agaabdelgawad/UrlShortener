using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using UrlShortener.ReadApi.DTOs;

namespace UrlShortener.ReadApi.Controllers
{
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

            //
            TimeSpan cacheExpiry = TimeSpan.FromHours(24);
            if (url.ExpirationDate.HasValue)
            {
                var timeUntilExpiration = url.ExpirationDate.Value - DateTime.UtcNow;
                cacheExpiry = timeUntilExpiration < cacheExpiry ? timeUntilExpiration : cacheExpiry;
            }
            //

            await _redisDb.StringSetAsync(shortUrl, url.OriginalUrl, cacheExpiry);

            return Redirect(url.OriginalUrl);
        }
    }
}
