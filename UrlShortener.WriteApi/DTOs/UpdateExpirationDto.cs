namespace UrlShortener.WriteApi.DTOs
{
    /// <summary>
    /// A lightweight DTO specifically for patch operations on the expiration date.
    /// </summary>
    public record UpdateExpirationDto(DateTime? NewExpirationDate);
}