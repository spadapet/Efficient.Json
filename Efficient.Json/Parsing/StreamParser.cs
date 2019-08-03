using System.IO;
using Efficient.Json.Itemizing;
using Efficient.Json.Tokenizing;

namespace Efficient.Json.Parsing
{
    /// <summary>
    /// Parses JSON from a stream
    /// </summary>
    internal sealed class StreamParser : ItemParser
    {
        public static JsonValue Parse(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true))
            {
                return StreamParser.Parse(reader);
            }
        }

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
