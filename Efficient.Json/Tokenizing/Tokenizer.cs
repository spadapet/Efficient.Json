using System.Diagnostics;
using System.Text;
using Efficient.Json.Tokens;
using Efficient.Json.Utility;

namespace Efficient.Json.Tokenizing
{
    /// <summary>
    /// Base class that can tokenize JSON text from any source.
    /// </summary>
    internal abstract class Tokenizer
    {
        protected int Line { get; private set; }
        protected int LineStart { get; private set; }
        private int pos;
        private char curChar;

        public abstract FullToken NextToken();
        protected abstract void SetTokenText(StringBuilder tokenBuffer);
        protected abstract char Peek();
        protected abstract char Read();

        protected Tokenizer()
        {
            this.pos = -1;
        }

        protected char NextChar()
        {
            this.pos++;
            return this.curChar = this.Read();
        }

        public static bool IsHexDigit(char ch)
        {
            if (ch >= '0' && ch <= '9')
            {
                return true;
            }

            ch |= (char)0x20;
            return ch >= 'a' && ch <= 'f';
        }

        public static bool IsDigit(char ch)
        {
            return ch >= '0' && ch <= '9';
        }

        protected Token ReadToken(StringBuilder tokenTextBuffer)
        {
            char ch = this.SkipSpacesAndComments(this.curChar);
            TokenType type = TokenType.Error;
            int start = this.pos;
            int hash = 0;

            switch (ch)
            {
                case '\0':
                    type = TokenType.None;
                    break;

                case 't':
                    if (this.NextChar() == 'r' &&
                        this.NextChar() == 'u' &&
                        this.NextChar() == 'e')
                    {
                        this.NextChar();
                        type = TokenType.True;
                        hash = StringHasher.TrueHash;
                    }
                    break;

                case 'f':
                    if (this.NextChar() == 'a' &&
                        this.NextChar() == 'l' &&
                        this.NextChar() == 's' &&
                        this.NextChar() == 'e')
                    {
                        this.NextChar();
                        type = TokenType.False;
                        hash = StringHasher.FalseHash;
                    }
                    break;

                case 'n':
                    if (this.NextChar() == 'u' &&
                        this.NextChar() == 'l' &&
                        this.NextChar() == 'l')
                    {
                        this.NextChar();
                        type = TokenType.Null;
                        hash = StringHasher.NullHash;
                    }
                    break;

                case '\"':
                    type = this.SkipString(tokenTextBuffer, ref hash);
                    if (type != TokenType.Error)
                    {
                        // Don't include quotes in the string token
                        return new Token(type, start + 1, this.pos - start - 2, hash);
                    }
                    break;

                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    type = this.SkipNumber(ch, tokenTextBuffer, ref hash);
                    break;

                case ',':
                    _ = this.NextChar();
                    type = TokenType.Comma;
                    break;

                case ':':
                    _ = this.NextChar();
                    type = TokenType.Colon;
                    break;

                case '{':
                    _ = this.NextChar();
                    type = TokenType.OpenCurly;
                    break;

                case '}':
                    _ = this.NextChar();
                    type = TokenType.CloseCurly;
                    break;

                case '[':
                    _ = this.NextChar();
                    type = TokenType.OpenBracket;
                    break;

                case ']':
                    _ = this.NextChar();
                    type = TokenType.CloseBracket;
                    break;
            }

            if (type == TokenType.Error)
            {
                // Move forward a bit so that the error message can show some context
                for (int i = 0; i < 16 && this.curChar != '\0'; i++)
                {
                    _ = this.NextChar();
                }
            }

            return new Token(type, start, this.pos - start, hash);
        }

        private TokenType SkipString(StringBuilder buffer, ref int hash)
        {
            TokenType type = TokenType.String;
            StringHasher hasher = StringHasher.Create();
            char ch = this.NextChar();

            while (true)
            {
                if (ch == '\"')
                {
                    _ = this.NextChar();
                    break;
                }
                else if (ch == '\\')
                {
                    type = TokenType.EncodedString;
                    hasher.AddChar(ch);
                    buffer?.Append(ch);
                    ch = this.NextChar();

                    switch (ch)
                    {
                        case '\"':
                        case '\\':
                        case '/':
                        case 'b':
                        case 'f':
                        case 'n':
                        case 'r':
                        case 't':
                            hasher.AddChar(ch);
                            buffer?.Append(ch);
                            ch = this.NextChar();
                            break;

                        case 'u':
                            hasher.AddChar(ch);
                            buffer?.Append(ch);
                            ch = this.NextChar();
                            if (!Tokenizer.IsHexDigit(ch))
                            {
                                return TokenType.Error;
                            }

                            hasher.AddChar(ch); // 1
                            buffer?.Append(ch);
                            ch = this.NextChar();
                            if (!Tokenizer.IsHexDigit(ch))
                            {
                                return TokenType.Error;
                            }

                            hasher.AddChar(ch); // 2
                            buffer?.Append(ch);
                            ch = this.NextChar();
                            if (!Tokenizer.IsHexDigit(ch))
                            {
                                return TokenType.Error;
                            }

                            hasher.AddChar(ch); // 3
                            buffer?.Append(ch);
                            ch = this.NextChar();
                            if (!Tokenizer.IsHexDigit(ch))
                            {
                                return TokenType.Error;
                            }

                            hasher.AddChar(ch); // 4
                            buffer?.Append(ch);
                            ch = this.NextChar();
                            break;

                        default:
                            return TokenType.Error;
                    }
                }
                else if (ch < ' ')
                {
                    return TokenType.Error;
                }
                else
                {
                    hasher.AddChar(ch);
                    buffer?.Append(ch);
                    ch = this.NextChar();
                }
            }

            hash = hasher.HashValue;
            Debug.Assert(buffer == null || StringHasher.HashSubstring(buffer.ToString(), 0, buffer.Length) == hash);

            return type;
        }

        private char SkipDigits(char ch, StringBuilder buffer, ref StringHasher hasher)
        {
            do
            {
                hasher.AddChar(ch);
                buffer?.Append(ch);
                ch = this.NextChar();
            }
            while (Tokenizer.IsDigit(ch));

            return ch;
        }

        private TokenType SkipNumber(char ch, StringBuilder buffer, ref int hash)
        {
            StringHasher hasher = StringHasher.Create();

            if (ch == '-')
            {
                hasher.AddChar(ch);
                buffer?.Append(ch);
                ch = this.NextChar();
            }

            if (!Tokenizer.IsDigit(ch))
            {
                return TokenType.Error;
            }

            ch = this.SkipDigits(ch, buffer, ref hasher);

            if (ch == '.')
            {
                hasher.AddChar(ch);
                buffer?.Append(ch);
                ch = this.NextChar();

                if (!Tokenizer.IsDigit(ch))
                {
                    return TokenType.Error;
                }

                ch = this.SkipDigits(ch, buffer, ref hasher);
            }

            if (ch == 'e' || ch == 'E')
            {
                hasher.AddChar(ch);
                buffer?.Append(ch);
                ch = this.NextChar();

                if (ch == '-' || ch == '+')
                {
                    hasher.AddChar(ch);
                    buffer?.Append(ch);
                    ch = this.NextChar();
                }

                if (!Tokenizer.IsDigit(ch))
                {
                    return TokenType.Error;
                }

                this.SkipDigits(ch, buffer, ref hasher);
            }

            hash = hasher.HashValue;
            Debug.Assert(buffer == null || StringHasher.HashSubstring(buffer.ToString(), 0, buffer.Length) == hash);

            return TokenType.Number;
        }

        private char SkipSpacesAndComments(char ch)
        {
            while (true)
            {
                switch (ch)
                {
                    case ' ':
                    case '\t':
                        ch = this.NextChar();
                        break;

                    case '\r':
                        if ((ch = this.NextChar()) != '\n')
                        {
                            this.Line++;
                            this.LineStart = this.pos;
                        }
                        break;

                    case '\n':
                        ch = this.NextChar();
                        this.Line++;
                        this.LineStart = this.pos;
                        break;

                    // Support C/C++ style comments. If they are actually in JSON, they are probably meant to be ignored and not treated like an error.
                    case '/':
                        switch (this.Peek())
                        {
                            case '/':
                                this.NextChar();
                                ch = this.NextChar();

                                while (ch != '\0' && ch != '\r' && ch != '\n')
                                {
                                    ch = this.NextChar();
                                }
                                break;

                            case '*':
                                this.NextChar();
                                ch = this.NextChar();

                                while (ch != '\0' && (ch != '*' || this.Peek() != '/'))
                                {
                                    switch (ch)
                                    {
                                        case '\r':
                                            if ((ch = this.NextChar()) != '\n')
                                            {
                                                this.Line++;
                                                this.LineStart = this.pos;
                                            }
                                            break;

                                        case '\n':
                                            ch = this.NextChar();
                                            this.Line++;
                                            this.LineStart = this.pos;
                                            break;

                                        default:
                                            ch = this.NextChar();
                                            break;
                                    }
                                }

                                if (ch != '\0')
                                {
                                    this.NextChar();
                                    ch = this.NextChar();
                                }
                                break;

                            default:
                                return ch;
                        }
                        break;

                    default:
                        return ch;
                }
            }
        }
    }
}
