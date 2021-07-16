using System;
using System.Collections.Generic;

namespace libGenerator
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Where<T>(this IEnumerable<T> src, Predicate<T> predicate)
        {
            foreach (var item in src)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }
        public static T[] ToArray<T>(this IEnumerable<T> src)
        {
            return new List<T>(src).ToArray();
        }
    }
}
