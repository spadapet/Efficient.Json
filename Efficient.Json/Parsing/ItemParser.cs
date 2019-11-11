using Efficient.Json.Itemizing;
using Efficient.Json.Tokens;
using System.Collections.Generic;
using System.Text;

namespace Efficient.Json.Parsing
{
    /// <summary>
    /// Parses JSON from items
    /// </summary>
    internal class ItemParser : Parser
    {
        private readonly Dictionary<Key, int> textBufferPositions;
        private readonly StringBuilder textBuffer;
        private const int TextBufferPositionsSize = 1024;
        private const int TextBufferSize = 1 << 16;

        protected ItemParser(Itemizer itemizer)
            : base(new JsonContext(null), itemizer)
        {
            this.textBufferPositions = new Dictionary<Key, int>(ItemParser.TextBufferPositionsSize, KeyComparer.Instance);
            this.textBuffer = new StringBuilder(ItemParser.TextBufferSize);
        }

        protected override string FinalText => this.textBuffer.ToString();

        protected override Token CacheText(FullToken fullToken)
        {
            Token token = fullToken.Token;

            switch (token.Type)
            {
                case TokenType.String:
                case TokenType.EncodedString:
                case TokenType.Number:
                    Key tokenKey = fullToken.DecodedKey;
                    if (!this.textBufferPositions.TryGetValue(tokenKey, out int start))
                    {
                        start = this.textBuffer.Length;
                        this.textBuffer.Append(tokenKey.Text);
                        this.textBufferPositions.Add(tokenKey, start);
                    }

                    token = new Token(token.Type == TokenType.Number ? TokenType.Number : TokenType.String, start, tokenKey.Text.Length, tokenKey.Hash);
                    break;

                default:
                    token = new Token(token.Type, 0, 0, 0);
                    break;
            }

            return token;
        }
    }
}
