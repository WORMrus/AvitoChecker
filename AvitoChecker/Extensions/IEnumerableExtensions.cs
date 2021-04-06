using System.Collections.Generic;

namespace AvitoChecker.Extensions
{
    public static class IEnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> list, System.Action<T> action)
        {
            foreach (T item in list)
                action(item);
        }
    }
}
