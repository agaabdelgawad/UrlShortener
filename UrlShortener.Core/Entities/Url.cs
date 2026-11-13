using System;
using System.Collections.Generic;
using System.Text;

namespace UrlShortener.Core.Entities
{
    public class Url
    {
        public long Id { get; set; } // The Primary Key (Sequence)
        public string ShortUrl { get; set; } = string.Empty; // Unique Base62 or Alias
        public string OriginalUrl { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpirationDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        // public string UserId { get; set; }
    }
}
