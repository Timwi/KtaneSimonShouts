using System;
using System.Collections.Generic;

namespace SimonShouts
{
    static class Ut
    {
        /// <summary>
        ///     Determines whether <paramref name="value"/> is between <paramref name="min"/> and <paramref name="max"/>
        ///     (inclusive).</summary>
        public static bool IsBetween(this int value, int min, int max) { return value >= min && value <= max; }

        /// <summary>
        ///     Instantiates a fully-initialized array with the specified dimensions.</summary>
        /// <param name="size">
        ///     Size of the first dimension.</param>
        /// <param name="initialiser">
        ///     Function to initialise the value of every element.</param>
        /// <typeparam name="T">
        ///     Type of the array element.</typeparam>
        public static T[] NewArray<T>(int size, Func<int, T> initialiser)
        {
            var result = new T[size];
            for (int i = 0; i < size; i++)
                result[i] = initialiser(i);
            return result;
        }

        public static T[] NewArray<T>(params T[] array) { return array; }

        public static int LastIndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var i = 0;
            var found = -1;
            foreach (var elem in source)
            {
                if (predicate(elem))
                    found = i;
                i++;
            }
            return found;
        }
    }
}
