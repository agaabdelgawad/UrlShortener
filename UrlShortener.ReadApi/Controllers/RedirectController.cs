using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;

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
            var query = "SELECT OriginalUrl FROM Urls WHERE ShortUrl = @ShortUrl";

            var originalUrl = await sqlConnection.QuerySingleOrDefaultAsync<string>(
                query, new { ShortUrl = shortUrl });

            if (originalUrl == null)
            {
                return NotFound(new { error = "Short URL not found." });
            }

            await _redisDb.StringSetAsync(shortUrl, originalUrl, TimeSpan.FromHours(24));

            return Redirect(originalUrl);
        }
    }
}
