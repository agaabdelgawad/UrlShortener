namespace UrlShortener.WriteApi.Settings
{
    using System.ComponentModel.DataAnnotations;

    public class UrlShortenerSettings
    {
        [Required(ErrorMessage = "BaseUrl is missing in configuration.")]
        public string BaseUrl { get; set; }

        [Required(ErrorMessage = "Hashids Salt is missing in configuration.")]
        public string HashidsSalt { get; set; }
    }
}
