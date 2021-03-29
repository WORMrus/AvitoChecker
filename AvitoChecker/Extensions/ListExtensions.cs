using System.Collections.Generic;

namespace AvitoChecker.Extensions
{
    public static class ListExtensions
    {
        public static void AddIfNotNull<T>(this ICollection<T> c, T value)
        {
            if (value != null) { c.Add(value); }
        }
    }
}
