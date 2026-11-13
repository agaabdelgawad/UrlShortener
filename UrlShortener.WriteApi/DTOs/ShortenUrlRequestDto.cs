using System.ComponentModel.DataAnnotations;

namespace UrlShortener.WriteApi.DTOs
{
    public record ShortenUrlRequestDto(
        [Required(ErrorMessage = "OriginalUrl is required.")]
        string OriginalUrl,

        [MaxLength(20, ErrorMessage = "Custom alias cannot exceed 20 characters.")]
        string? CustomAlias,

        DateTime? ExpirationDate);
}
