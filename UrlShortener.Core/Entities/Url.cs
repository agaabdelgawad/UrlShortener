namespace UrlShortener.Core.Entities
{

    /// <summary>
    /// Represents the core domain entity for a shortened URL.
    /// Manages the mapping between the original long URL and its short Base62/Alias equivalent.
    /// </summary>
    public class Url
    {
        /// <summary>
        /// The primary key generated via a high-performance SQL Sequence to avoid EF Core round-trips.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The unique identifier used in the route. Can be an auto-generated Base62 string or a user-defined custom alias.
        /// </summary>
        public string ShortUrl { get; set; } = string.Empty;

        public string OriginalUrl { get; set; } = string.Empty;

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional timestamp to automatically expire the link. 
        /// Used for calculating Redis TTL and enforcing 410 Gone responses in the Read API.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Implements the Soft Delete pattern. True means the link is logically deleted, 
        /// invalidating its cache and preventing redirects.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
