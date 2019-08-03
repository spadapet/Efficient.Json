using System.Collections.Generic;

namespace Efficient.Json.Value
{
    /// <summary>
    /// JsonValues can share the same ValueData, this comparer is used to find a match
    /// </summary>
    internal class ValueDataComparer : IEqualityComparer<ValueData>
    {
        public static ValueDataComparer Instance { get; } = new ValueDataComparer();

        bool IEqualityComparer<ValueData>.Equals(ValueData x, ValueData y)
        {
            return x.Equals(y);
        }

        int IEqualityComparer<ValueData>.GetHashCode(ValueData obj)
        {
            return obj.GetHashCode();
        }
    }
}
