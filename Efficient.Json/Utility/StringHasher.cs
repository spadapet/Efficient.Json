using System.Diagnostics;

namespace Efficient.Json.Utility
{
    /// <summary>
    /// Hashes substrings (something .NET can't do)
    /// </summary>
    internal struct StringHasher
    {
        private int state;
        private int hash1;
        private int hash2;

        private const int InitialHash = 5381;
        private const int HashShift = 5;
        private const int HashMultiplier = 1566083941;
        private const int HashState1 = 3;
        private const int HashState2 = 2;

        public static readonly int TrueHash = StringHasher.HashSubstring("true", 0, 4);
        public static readonly int FalseHash = StringHasher.HashSubstring("false", 0, 5);
        public static readonly int NullHash = StringHasher.HashSubstring("null", 0, 4);
        public static readonly int EmptyHash = StringHasher.HashSubstring(string.Empty, 0, 0);

        private StringHasher(int state, int hash1)
        {
            this.state = state;
            this.hash1 = hash1;
            this.hash2 = StringHasher.InitialHash;
        }

        public static StringHasher Create()
        {
            return new StringHasher(StringHasher.HashState1, StringHasher.InitialHash);
        }

        public static StringHasher Create(char firstChar)
        {
            return new StringHasher(StringHasher.HashState2, ((StringHasher.InitialHash << StringHasher.HashShift) + StringHasher.InitialHash) ^ firstChar);
        }

        public void AddChar(char nextChar)
        {
            Debug.Assert(this.state != 0);
            int hash = ((this.hash2 << StringHasher.HashShift) + this.hash2) ^ nextChar;

            this.state ^= 1;
            this.hash2 = this.hash1;
            this.hash1 = hash;
        }

        public int HashValue
        {
            get
            {
                return
                    (this.hash2 * StringHasher.HashMultiplier + this.hash1) * (this.state ^ StringHasher.HashState1) +
                    (this.hash1 * StringHasher.HashMultiplier + this.hash2) * (this.state ^ StringHasher.HashState2);
            }
        }

        /// <summary>
        /// Ends up with the same result as StringHasher
        /// </summary>
        public static int HashSubstring(string str, int start, int length)
        {
            int end = start + length;
            int pos = start;
            int hash1 = StringHasher.InitialHash;
            int hash2 = StringHasher.InitialHash;

            for (; pos + 1 < end; pos += 2)
            {
                unchecked
                {
                    hash1 = ((hash1 << StringHasher.HashShift) + hash1) ^ str[pos];
                    hash2 = ((hash2 << StringHasher.HashShift) + hash2) ^ str[pos + 1];
                }
            }

            if (pos < end)
            {
                hash1 = ((hash1 << StringHasher.HashShift) + hash1) ^ str[pos];
            }

            int hash = hash1 + (hash2 * StringHasher.HashMultiplier);
            return hash;
        }

        public static int Hash(string str)
        {
            return StringHasher.HashSubstring(str, 0, str.Length);
        }
    }
}
