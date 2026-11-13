namespace UrlShortener.ReadApi.DTOs
{
    /// <summary>
    /// A lightweight, immutable projection mapped via Dapper.
    /// Retrieves strictly the minimum fields required for validation and routing, avoiding heavy ORM overhead.
    /// </summary>
    public record UrlResponseDto(string OriginalUrl, DateTime? ExpirationDate, bool IsDeleted);
}