using System.IO;
using System.Text;
using Efficient.Json.Tokens;

namespace Efficient.Json.Tokenizing
{
    /// <summary>
    /// Provides text to the JSON tokenizer that comes from a stream
    /// </summary>
    internal sealed class StreamTokenizer : Tokenizer
    {
        private readonly TextReader reader;
        private readonly StringBuilder tokenBuffer;
        private readonly char[] buffer;
        private string tokenText;
        private int bufferEnd;
        private int bufferPos;

        private const int BufferSize = 8192;
        private const int TokenBufferSize = 256;

        public StreamTokenizer(TextReader reader)
        {
            this.reader = reader;
            this.tokenBuffer = new StringBuilder(StreamTokenizer.TokenBufferSize);
            this.buffer = new char[StreamTokenizer.BufferSize];
            this.bufferPos = -1;
            this.NextChar();
        }

        public override FullToken NextToken()
        {
            Token token = this.ReadToken(this.tokenBuffer);
            this.tokenText = this.tokenBuffer.ToString();
            this.tokenBuffer.Clear();

            return new FullToken(new Token(token.Type, 0, token.Length, token.Hash), this.tokenText, this.Line, token.Start - this.LineStart);
        }

        protected override void SetTokenText(StringBuilder tokenBuffer)
        {
            this.tokenText = tokenBuffer.ToString();
            tokenBuffer.Clear();
        }

        protected override char Peek()
        {
            if (this.bufferPos + 1 != this.bufferEnd)
            {
                return this.buffer[this.bufferPos + 1];
            }
            else if (this.FillBuffer() == 0)
            {
                this.bufferPos = -1;
                return '\0';
            }
            else
            {
                this.bufferPos = -1;
                return this.buffer[0];
            }
        }

        protected override char Read()
        {
            if (++this.bufferPos == this.bufferEnd && this.FillBuffer() == 0)
            {
                return '\0';
            }

            return this.buffer[this.bufferPos];
        }

        private int FillBuffer()
        {
            this.bufferPos = 0;
            return this.bufferEnd = this.reader.Read(this.buffer, 0, StreamTokenizer.BufferSize);
        }
    }
}
