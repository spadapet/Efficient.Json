using System.Collections.Generic;

namespace Efficient.Json.Tokens
{
    internal class KeyComparer : IEqualityComparer<Key>
    {
        public static KeyComparer Instance { get; } = new KeyComparer();

        public static bool AreEqual(Key x, Key y)
        {
            return x.Hash == y.Hash && x.Text == y.Text;
        }

        bool IEqualityComparer<Key>.Equals(Key x, Key y)
        {
            return KeyComparer.AreEqual(x, y);
        }

        int IEqualityComparer<Key>.GetHashCode(Key obj)
        {
            return obj.Hash;
        }
    }
}
