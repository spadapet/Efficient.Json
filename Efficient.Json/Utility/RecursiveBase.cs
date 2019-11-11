using Efficient.Json.Tokens;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Efficient.Json.Utility
{
    /// <summary>
    /// Base class for anything that handles JSON tokens
    /// </summary>
    internal abstract class RecursiveBase
    {
        private readonly Stack<Key> jsonPath;
        private static readonly string HashIsIndex = "<HashIsIndex>";

        protected RecursiveBase()
        {
            this.jsonPath = new Stack<Key>(Constants.BufferSize);
        }

        protected void PushJsonPath(Key key)
        {
            this.jsonPath.Push(key);
        }

        protected void PushJsonPath(int index)
        {
            this.jsonPath.Push(new Key(RecursiveBase.HashIsIndex, index));
        }

        protected void PopJsonPath()
        {
            this.jsonPath.Pop();
        }

        protected string CurrentJsonPath
        {
            get
            {
                StringBuilder sb = new StringBuilder("@");

                foreach (Key key in this.jsonPath.Reverse())
                {
                    sb.Append(object.ReferenceEquals(key.Text, RecursiveBase.HashIsIndex)
                        ? $"[{key.Hash}]"
                        : $".{key.Text}");
                }

                return sb.ToString();
            }
        }

        protected JsonException ParseException(FullToken token, string message, params object[] args)
        {
            message = string.Format(CultureInfo.CurrentCulture, message, args);

            string tokenText = token.EncodedText;
            if (tokenText.Length > 32)
            {
                tokenText = tokenText.Substring(0, 32) + "..." + (tokenText[0] == '\"' ? "\"" : string.Empty);
            }

            return new JsonException(token, string.Format(CultureInfo.CurrentCulture, Resources.Parser_Message,
                token.Line + 1, // {0}
                token.Column + 1, // {1}
                tokenText, // {2}
                this.CurrentJsonPath, // {3}
                message)); // {4}
        }
    }
}
