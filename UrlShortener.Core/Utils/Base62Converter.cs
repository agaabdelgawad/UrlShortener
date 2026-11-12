using System;
using System.Collections.Generic;
using System.Text;

namespace UrlShortener.Core.Utils
{
    public static class Base62Converter
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly int Base = Alphabet.Length;

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
