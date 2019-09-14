using System.Globalization;
using System.IO;
using System.Text;
using Efficient.Json.Tokens;

namespace Efficient.Json.Utility
{
    internal static class EncodingUtility
    {
        public static string EncodeToken(FullToken token)
        {
            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                EncodingUtility.EncodeToken(token, writer);
                return writer.ToString();
            }
        }

        public static void EncodeTokenAsString(FullToken token, TextWriter writer)
        {
            if (!token.HasType(TokenType.AnyValue))
            {
                throw JsonException.New(Resources.Convert_KeyStringFailed, token.Type);
            }

            if (!token.HasType(TokenType.AnyString))
            {
                writer.Write('\"');
            }

            EncodingUtility.EncodeToken(token.FullText, token.Token, writer);

            if (!token.HasType(TokenType.AnyString))
            {
                writer.Write('\"');
            }
        }

        public static void EncodeToken(FullToken token, TextWriter writer)
        {
            EncodingUtility.EncodeToken(token.FullText, token.Token, writer);
        }

        public static void EncodeToken(string text, Token token, TextWriter writer)
        {
            switch (token.Type)
            {
                case TokenType.OpenCurly:
                    writer.Write("{");
                    return;

                case TokenType.CloseCurly:
                    writer.Write("}");
                    return;

                case TokenType.OpenBracket:
                    writer.Write("[");
                    return;

                case TokenType.CloseBracket:
                    writer.Write("]");
                    return;

                case TokenType.Colon:
                    writer.Write(":");
                    return;

                case TokenType.Comma:
                    writer.Write(",");
                    return;

                case TokenType.False:
                    writer.Write("false");
                    return;

                case TokenType.True:
                    writer.Write("true");
                    return;

                case TokenType.Null:
                    writer.Write("null");
                    return;

                case TokenType.EncodedString:
                    if (text != null)
                    {
                        writer.Write($"\"{text.Substring(token.Start, token.Length)}\"");
                    }
                    else
                    {
                        writer.Write($"<{token.Type}>");
                    }
                    return;

                case TokenType.String:
                    if (text == null)
                    {
                        writer.Write($"<{token.Type}>");
                    }
                    // else code is below
                    break;

                default:
                    if (text != null && token.Length > 0)
                    {
                        writer.Write(text.Substring(token.Start, token.Length));
                    }
                    else
                    {
                        writer.Write($"<{token.Type}>");
                    }
                    return;
            }

            writer.Write('\"');

            for (int i = token.Start; i < token.Start + token.Length; i++)
            {
                char ch = text[i];
                switch (ch)
                {
                    case '\"':
                        writer.Write("\\\"");
                        break;

                    case '\\':
                        writer.Write("\\\\");
                        break;

                    case '\b':
                        writer.Write("\\b");
                        break;

                    case '\f':
                        writer.Write("\\f");
                        break;

                    case '\n':
                        writer.Write("\\n");
                        break;

                    case '\r':
                        writer.Write("\\r");
                        break;

                    case '\t':
                        writer.Write("\\t");
                        break;

                    default:
                        if (ch >= ' ')
                        {
                            writer.Write(ch);
                        }
                        break;
                }
            }

            writer.Write('\"');
        }

        public static Key DecodeToken(string text, Token token)
        {
            if (token.Type != TokenType.EncodedString)
            {
                return new Key(text.Substring(token.Start, token.Length), token.Hash);
            }

            int cur = token.Start;
            StringBuilder value = new StringBuilder(token.Length);
            StringHasher hasher = StringHasher.Create();

            for (int end = cur + token.Length; cur != -1 && cur < end;)
            {
                char ch = text[cur];
                if (ch == '\\')
                {
                    ch = (cur + 1 < end) ? text[cur + 1] : '\0';
                    switch (ch)
                    {
                        case '\"':
                        case '\\':
                        case '/':
                            hasher.AddChar(ch);
                            value.Append(ch);
                            cur += 2;
                            break;

                        case 'b':
                            hasher.AddChar('\b');
                            value.Append('\b');
                            cur += 2;
                            break;

                        case 'f':
                            hasher.AddChar('\f');
                            value.Append('\f');
                            cur += 2;
                            break;

                        case 'n':
                            hasher.AddChar('\n');
                            value.Append('\n');
                            cur += 2;
                            break;

                        case 'r':
                            hasher.AddChar('\r');
                            value.Append('\r');
                            cur += 2;
                            break;

                        case 't':
                            hasher.AddChar('\t');
                            value.Append('\t');
                            cur += 2;
                            break;

                        case 'u':
                            if (cur + 5 < end)
                            {
                                string buffer = text.Substring(cur + 2, 4);
                                if (uint.TryParse(buffer, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint decoded))
                                {
                                    ch = (char)decoded;
                                    hasher.AddChar(ch);
                                    value.Append(ch);
                                    cur += 6;
                                }
                                else
                                {
                                    cur = -1;
                                }
                            }
                            else
                            {
                                cur = -1;
                            }
                            break;

                        default:
                            cur = -1;
                            break;
                    }
                }
                else
                {
                    hasher.AddChar(ch);
                    value.Append(ch);
                    cur++;
                }
            }

            if (cur == -1)
            {
                throw JsonException.New(Resources.Parser_InvalidStringToken);
            }

            return new Key(value.ToString(), hasher.HashValue);
        }
    }
}
