using System;
using System.Diagnostics;
using Efficient.Json.Tokens;
using Efficient.Json.Utility;

namespace Efficient.Json.Value
{
    /// <summary>
    /// Parsed data for any JsonValue
    /// </summary>
    [DebuggerDisplay("{Type}")]
    internal struct ValueData : IEquatable<ValueData>
    {
        public Token Token { get; }
        public ValueList List { get; }
        public JsonContext Context => this.List.Context;

        public ValueData(Token token, ValueList list)
        {
            this.Token = token;
            this.List = list;
        }

        public override int GetHashCode() => HashUtility.CombineHashCodes(
            this.List.GetHashCode(),
            this.Token.Type.GetHashCode(),
            this.Token.Length.GetHashCode(),
            this.Token.Hash);

        public bool Equals(ValueData other)
        {
            if (this.List != other.List ||
                this.Token.Type != other.Token.Type ||
                this.Token.Length != other.Token.Length ||
                this.Token.Hash != other.Token.Hash)
            {
                return false;
            }

            JsonContext context = this.Context;
            string text = context.Text;

            if (text != null)
            {
                return string.CompareOrdinal(text, this.Token.Start, text, other.Token.Start, this.Token.Length) == 0;
            }
            else
            {
                // During a streaming parse, all tokens with the same text are put at the same location in the text buffer
                return this.Token.Start == other.Token.Start;
            }
        }

        public override bool Equals(object obj) => obj is ValueData other && this.Equals(other);
        public static bool operator ==(ValueData x, ValueData y) => x.Equals(y);
        public static bool operator !=(ValueData x, ValueData y) => !x.Equals(y);
    }
}
