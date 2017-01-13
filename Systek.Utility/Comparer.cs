using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Utility
{
    /// <summary>
    /// Utility for comparing various special types.
    /// </summary>
    public static class Comparer
    {
        /// <summary>
        /// Compares two IDictionary objects for equality
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="first">The first IDictionary to be compared.</param>
        /// <param name="second">The other IDictionary to be compared.</param>
        /// <returns></returns>
        public static bool DictionaryEqual<TKey, TValue>(
            this Dictionary<TKey, TValue> first, Dictionary<TKey, TValue> second)
        {
            return first.DictionaryEqual(second, null);
        }

        /// <summary>
        /// Compares two IDictionary objects for equality
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="first">The first IDictionary to be compared.</param>
        /// <param name="second">The other IDictionary to be compared.</param>
        /// <param name="valueComparer">The value comparer for the specific IDictionary key/value combo type.</param>
        /// <returns></returns>
        public static bool DictionaryEqual<TKey, TValue>(
            this Dictionary<TKey, TValue> first, Dictionary<TKey, TValue> second,
            IEqualityComparer<TValue> valueComparer)
        {
            if (first == second) return true;
            if ((first == null) || (second == null)) return false;
            if (first.Count != second.Count) return false;

            valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            foreach (var kvp in first)
            {
                TValue secondValue;
                if (!second.TryGetValue(kvp.Key, out secondValue)) return false;
                if (!valueComparer.Equals(kvp.Value, secondValue)) return false;
            }
            return true;
        }
    }
}
