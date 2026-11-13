namespace UrlShortener.Core.Utils
{

    /// <summary>
    /// Provides utility methods for Base62 encoding.
    /// Base62 uses [a-z, A-Z, 0-9] characters, making the resulting string entirely URL-safe and compact.
    /// </summary>
    public static class Base62Converter
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly int Base = Alphabet.Length;

        /// <summary>
        /// Encodes a numeric sequence ID into a short Base62 string.
        /// </summary>
        /// <param name="value">The sequential ID generated directly from the database sequence.</param>
        /// <returns>A compact, URL-safe Base62 encoded string.</returns>
        public static string Encode(long value)
        {
            if (value == 0) return Alphabet[0].ToString();

            var result = new Stack<char>();

            while (value > 0)
            {
                result.Push(Alphabet[(int)(value % Base)]);
                value /= Base;
            }

            return new string(result.ToArray());
        }
    }
}
