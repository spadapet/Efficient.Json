using System.Diagnostics;

namespace Efficient.Json.Tokens
{
    /// <summary>
    /// Output from the tokenizer
    /// </summary>
    [DebuggerDisplay("{Type}")]
    internal struct Token
    {
        public readonly TokenType Type;
        public readonly int Start;
        public readonly int Length;
        public readonly int Hash;

        public Token(TokenType type, int start, int length, int hash)
        {
            this.Type = type;
            this.Start = start;
            this.Length = length;
            this.Hash = hash;
        }
    }
}
