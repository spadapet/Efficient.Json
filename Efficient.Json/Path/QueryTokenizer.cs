using Efficient.Json.Tokenizing;

namespace Efficient.Json.Path
{
    internal class QueryTokenizer
    {
        private readonly string text;
        private readonly int end;
        private int pos;
        private char curChar;

        public QueryTokenizer(string text)
        {
            this.text = text;
            this.end = this.text.Length;
            this.pos = -1;
            this.NextChar();
        }

        public QueryToken NextToken() => this.ReadToken();
        private char Peek() => (this.pos + 1 != this.end) ? this.text[this.pos + 1] : '\0';
        private char Read() => (++this.pos != this.end) ? this.text[this.pos] : '\0';

        private char NextChar()
        {
            this.pos++;
            return this.curChar = this.Read();
        }

        private QueryToken ReadToken()
        {
            char ch = this.SkipSpaces(this.curChar);
            QueryTokenType type = QueryTokenType.Error;
            int start = this.pos;

            switch (ch)
            {
                case '\0':
                    type = QueryTokenType.None;
                    break;

                case '\"':
                case '\'':
                    type = this.SkipString(ch);
                    if (type != QueryTokenType.Error)
                    {
                        // Don't include quotes in the string token
                        return new QueryToken(type, start + 1, this.pos - start - 2);
                    }
                    break;

                case '@':
                case '$':
                    switch (this.Peek())
                    {
                        case '.':
                        case '[':
                            _ = this.NextChar();
                            type = (ch == '@') ? QueryTokenType.AtRef : QueryTokenType.DollarRef;
                            break;

                        default:
                            type = this.SkipIdentifier();
                            break;
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
                    type = this.SkipNumber(ch);
                    break;

                default:
                    if (QueryTokenizer.IsIdentifierStart(ch))
                    {
                        type = this.SkipIdentifier();
                    }
                    break;
            }

            return new QueryToken(type, start, this.pos - start);
        }

        private QueryTokenType SkipString(char quoteChar)
        {
            QueryTokenType type = QueryTokenType.String;
            char ch = this.NextChar();

            while (true)
            {
                if (ch == quoteChar)
                {
                    _ = this.NextChar();
                    break;
                }
                else if (ch == '\\')
                {
                    type = QueryTokenType.EncodedString;
                    ch = this.NextChar();

                    switch (ch)
                    {
                        case '\"':
                        case '\'':
                        case '\\':
                        case '/':
                        case 'b':
                        case 'f':
                        case 'n':
                        case 'r':
                        case 't':
                            ch = this.NextChar();
                            break;

                        case 'u':
                            if (!Tokenizer.IsHexDigit(this.NextChar()) ||
                                !Tokenizer.IsHexDigit(this.NextChar()) ||
                                !Tokenizer.IsHexDigit(this.NextChar()) ||
                                !Tokenizer.IsHexDigit(this.NextChar()))
                            {
                                return QueryTokenType.Error;
                            }

                            ch = this.NextChar();
                            break;

                        default:
                            return QueryTokenType.Error;
                    }
                }
                else if (ch < ' ')
                {
                    return QueryTokenType.Error;
                }
                else
                {
                    ch = this.NextChar();
                }
            }

            return type;
        }

        private char SkipDigits()
        {
            char ch;
            do
            {
                ch = this.NextChar();
            }
            while (Tokenizer.IsDigit(ch));

            return ch;
        }

        private QueryTokenType SkipNumber(char ch)
        {
            if (ch == '-')
            {
                ch = this.NextChar();
            }

            if (!Tokenizer.IsDigit(ch))
            {
                return QueryTokenType.Error;
            }

            ch = this.SkipDigits();

            if (ch == '.')
            {
                ch = this.NextChar();

                if (!Tokenizer.IsDigit(ch))
                {
                    return QueryTokenType.Error;
                }

                ch = this.SkipDigits();
            }

            if (ch == 'e' || ch == 'E')
            {
                ch = this.NextChar();

                if (ch == '-' || ch == '+')
                {
                    ch = this.NextChar();
                }

                if (!Tokenizer.IsDigit(ch))
                {
                    return QueryTokenType.Error;
                }

                this.SkipDigits();
            }

            return QueryTokenType.Number;
        }

        private static bool IsIdentifierStart(char ch)
        {
            switch (ch)
            {
                case '_':
                case '$':
                case '@':
                case '%':
                    return true;

                default:
                    return char.IsLetter(ch);
            }
        }

        private static bool IsIdentifier(char ch)
        {
            switch (ch)
            {
                case '-':
                    return true;

                default:
                    return QueryTokenizer.IsIdentifierStart(ch) || char.IsDigit(ch);
            }
        }

        private QueryTokenType SkipIdentifier()
        {
            for (char ch = this.NextChar(); QueryTokenizer.IsIdentifier(ch);)
            {
                _ = this.NextChar();
            }

            return QueryTokenType.Identifier;
        }

        private char SkipSpaces(char ch)
        {
            while (true)
            {
                switch (ch)
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        ch = this.NextChar();
                        break;

                    default:
                        return ch;
                }
            }
        }
    }
}
