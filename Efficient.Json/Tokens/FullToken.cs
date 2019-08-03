using System.Diagnostics;
using Efficient.Json.Utility;

namespace Efficient.Json.Tokens
{
    /// <summary>
    /// Output from the tokenizer
    /// </summary>
    [DebuggerDisplay("{Token}")]
    internal struct FullToken
    {
        public static FullToken Empty { get; } = new FullToken(new Token(TokenType.None, 0, 0, 0), string.Empty, 0, 0);

        public readonly Token Token;
        public readonly string FullText;
        public readonly int Line;
        public readonly int Column;
        public TokenType Type => this.Token.Type;
        public bool HasType(TokenType type) => (this.Type & type) != TokenType.None;
        public Key DecodedKey => EncodingUtility.DecodeToken(this.FullText, this.Token);
        public string EncodedText => EncodingUtility.EncodeToken(this);

        public FullToken(Token token, string fullText, int line, int column)
        {
            this.Token = token;
            this.FullText = fullText;
            this.Line = line;
            this.Column = column;
        }
    }
}
