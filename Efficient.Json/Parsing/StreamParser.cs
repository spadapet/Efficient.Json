using Efficient.Json.Itemizing;
using Efficient.Json.Tokenizing;
using System.IO;

namespace Efficient.Json.Parsing
{
    /// <summary>
    /// Parses JSON from a stream
    /// </summary>
    internal sealed class StreamParser : ItemParser
    {
        public static JsonValue Parse(TextReader reader)
        {
            StreamParser parser = new StreamParser(reader);
            return parser.ParseRoot();
        }

        private StreamParser(TextReader reader)
            : base(new TokenItemizer(new StreamTokenizer(reader)))
        {
        }
    }
}
