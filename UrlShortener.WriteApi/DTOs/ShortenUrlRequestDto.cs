using System.ComponentModel.DataAnnotations;

namespace UrlShortener.WriteApi.DTOs
{
    /// <summary>
    /// An immutable Data Transfer Object (DTO) for incoming link creation requests.
    /// Utilizes Data Annotations to enforce early validation at the controller edge, rejecting bad requests before hitting the database.
    /// </summary>
    public record ShortenUrlRequestDto(
        [Required(ErrorMessage = "OriginalUrl is required.")]
    string OriginalUrl,

        [MaxLength(20, ErrorMessage = "Custom alias cannot exceed 20 characters.")]
    string? CustomAlias,

        DateTime? ExpirationDate);
}
