namespace System.Collections.Generic;

public static class DictinaryExtensions
{
    public static Dictionary<Key, Value>? MergeInPlace<Key, Value>(this Dictionary<Key, Value>? left, Dictionary<Key, Value>? right)
    {
        if (left == null)
            throw new ArgumentNullException("Can't merge into a null dictionary");

        if (right == null)
            return left;

        foreach (var kvp in right)
            if (!left.ContainsKey(kvp.Key))
                left.Add(kvp.Key, kvp.Value);

        return left;
    }
}
