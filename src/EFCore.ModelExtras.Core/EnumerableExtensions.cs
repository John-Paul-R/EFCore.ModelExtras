using System;
using System.Collections.Generic;

namespace EFCore.ModelExtras.Core;

internal static class EnumerableExtensions
{
    /// <summary>
    /// Splits a sequence into two lists based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence to split.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>A tuple containing two lists: the first with elements that match the predicate,
    /// and the second with elements that don't match.</returns>
    public static (List<T> Matching, List<T> NotMatching) SplitOn<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        var matching = new List<T>();
        var notMatching = new List<T>();

        foreach (var item in source) {
            if (predicate(item)) {
                matching.Add(item);
            } else {
                notMatching.Add(item);
            }
        }

        return (matching, notMatching);
    }
}
