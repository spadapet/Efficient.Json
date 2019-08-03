using System.Collections.Generic;

namespace Efficient.Json.Tokens
{
    internal class FullTokenComparer : IEqualityComparer<FullToken>
    {
        public static FullTokenComparer Instance { get; } = new FullTokenComparer();

        public static bool AreEqual(FullToken x, FullToken y)
        {
            return x.Token.Hash == y.Token.Hash &&
                x.Token.Length == y.Token.Length &&
                x.Token.Type == y.Token.Type &&
                string.CompareOrdinal(x.FullText, x.Token.Start, y.FullText, y.Token.Start, y.Token.Length) == 0;
        }

        bool IEqualityComparer<FullToken>.Equals(FullToken x, FullToken y)
        {
            return FullTokenComparer.AreEqual(x, y);
        }

        int IEqualityComparer<FullToken>.GetHashCode(FullToken obj)
        {
            return obj.Token.Hash;
        }
    }
}
