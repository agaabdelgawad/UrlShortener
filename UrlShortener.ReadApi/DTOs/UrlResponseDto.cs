namespace UrlShortener.ReadApi.DTOs
{
    public record UrlResponseDto(string OriginalUrl, DateTime? ExpirationDate, bool IsDeleted);
}
