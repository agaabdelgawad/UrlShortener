using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Utils;
using UrlShortener.WriteApi.Data;
using UrlShortener.WriteApi.DTOs;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace UrlShortener.WriteApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrlsController (AppDbContext _context, IConfiguration _configuration) : ControllerBase
    {
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
            // Scenario 2: Auto-generate Short URL using Base62 with Collision Handling
            else
            {
                bool isUnique = false;
                int maxRetries = 5;
                int attempts = 0;

                while (!isUnique && attempts < maxRetries)
                {
                    urlId = await GetNextSequenceValueAsync();
                    generatedShortUrl = Base62Converter.Encode(urlId);

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

            var baseUrl = _configuration["UrlShortenerSettings:BaseUrl"];
            var finalUrl = $"{baseUrl}{generatedShortUrl}";

            return Ok(new
            {
                shortUrl = finalUrl,
                originalUrl = request.OriginalUrl,
                expirationDate = request.ExpirationDate
            });
        }

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

                if (nextValue < 100_000_000_000)
                {
                    throw new InvalidOperationException($"Critical Error: Invalid sequence value generated: {nextValue}");
                }
            }
            finally
            {
                await connection.CloseAsync();
            }

            return nextValue;
        }
    }
}
