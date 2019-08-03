using System.Text;
using Efficient.Json.Tokens;

namespace Efficient.Json.Tokenizing
{
    /// <summary>
    /// Provides text to the JSON tokenizer that comes from a string
    /// </summary>
    internal sealed class StringTokenizer : Tokenizer
    {
        private readonly string text;
        private readonly int end;
        private int pos;

        public StringTokenizer(string text)
        {
            this.text = text;
            this.end = this.text.Length;
            this.pos = -1;
            this.NextChar();
        }

        public override FullToken NextToken()
        {
            Token token = this.ReadToken(null);
            return new FullToken(token, this.text, this.Line, token.Start - this.LineStart);
        }

        protected override void SetTokenText(StringBuilder tokenBuffer) { }
        protected override char Peek() => (this.pos + 1 != this.end) ? this.text[this.pos + 1] : '\0';
        protected override char Read() => (++this.pos != this.end) ? this.text[this.pos] : '\0';
    }
}
