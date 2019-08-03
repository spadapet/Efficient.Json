using System.Diagnostics;
using Efficient.Json.Utility;

namespace Efficient.Json.Tokens
{
    /// <summary>
    /// Caches the hash of a string, helpful for keys in a dictionary. The tokenizer hashes strings as it sees them,
    /// so this also prevents ever re-reading a string just to compute its hash.
    /// </summary>
    [DebuggerDisplay("{Text} [{Hash}]")]
    internal struct Key
    {
        public readonly string Text;
        public readonly int Hash;

        public Key(string text)
        {
            this.Text = text;
            this.Hash = StringHasher.HashSubstring(text, 0, text.Length);
        }

        public Key(string text, int hash)
        {
            this.Text = text;
            this.Hash = hash;
        }

        public FullToken FullToken => new FullToken(new Token(TokenType.String, 0, this.Text.Length, this.Hash), this.Text, 0, 0);
    }
}
