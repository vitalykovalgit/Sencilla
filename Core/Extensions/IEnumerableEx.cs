
namespace System.Collections.Generic;

/// <summary>
/// 
/// </summary>
public static class IEnumerableEx
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="action"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (action == null) throw new ArgumentNullException("action");

        foreach (T item in source)
            action(item);
    }

    /// <summary>
    /// Check if first araay begins with second
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns> True if first array begins with second, or second array is empty </returns>
    public static bool StartWith<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
        var fEnum = first.GetEnumerator();
        var sEnum = second.GetEnumerator();
        while (sEnum.MoveNext())
        {
            if (!fEnum.MoveNext())
                return false;

            if (sEnum.Current == null)
            {
                if (fEnum.Current == null)
                    continue;

                return false;
            }

            if (!sEnum.Current.Equals(fEnum.Current))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if first araay begins with second
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns> True if first array begins with second, or second array is empty </returns>
    public static bool StartWith<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector)
    {
        var sEnum = second.GetEnumerator();
        var fEnum = first.GetEnumerator();
        while (sEnum.MoveNext())
        {
            if (!fEnum.MoveNext())
                return false;

            if (sEnum.Current == null)
            {
                if (fEnum.Current == null)
                    continue;

                return false;
            }

            if (!sEnum.Current.Equals(keySelector(fEnum.Current)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if first araay begins with second
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns> True if first array begins with second, or second array is empty </returns>
    public static bool StartWith<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, bool> comparer)
    {
        var secEnum = second.GetEnumerator();
        var fstEnum = first.GetEnumerator();
        while (secEnum.MoveNext())
        {
            if (!fstEnum.MoveNext())
                return false;

            if (!comparer(fstEnum.Current, secEnum.Current))
                return false;
        }

        return true;
    }
}
