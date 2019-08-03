using Efficient.Json.Itemizing;
using Efficient.Json.Tokenizing;

namespace Efficient.Json.Parsing
{
    /// <summary>
    /// Parses JSON from a string
    /// </summary>
    internal sealed class StringParser : Parser
    {
        public static JsonValue Parse(string text)
        {
            StringParser parser = new StringParser(text ?? string.Empty);
            return parser.ParseRoot();
        }

        private StringParser(string text)
            : base(new JsonContext(text), new TokenItemizer(new StringTokenizer(text)))
        {
        }
    }
}
