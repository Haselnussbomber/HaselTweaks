using System.Collections.Generic;
using System.Linq;

namespace HaselTweaks.Extensions;

public static class DictionaryExtensions
{
    //! https://www.codeproject.com/Tips/494499/Implementing-Dictionary-RemoveAll
    public static void RemoveAll<K, V>(this IDictionary<K, V> dict, Func<K, V, bool> match, bool dispose = false)
    {
        foreach (var key in dict.Keys.ToArray())
        {
            if (!dict.TryGetValue(key, out var value) || !match(key, value))
                continue;

            if (dispose && value is IDisposable disposable)
                disposable.Dispose();

            dict.Remove(key);
        }
    }

    public static void Dispose<K, V>(this IDictionary<K, V> dict) where V : IDisposable
    {
        foreach (var value in dict.Values)
        {
            value.Dispose();
        }

        dict.Clear();
    }
}
