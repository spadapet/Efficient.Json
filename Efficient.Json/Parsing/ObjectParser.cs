using Efficient.Json.Itemizing;

namespace Efficient.Json.Parsing
{
    /// <summary>
    /// Parses JSON from an object
    /// </summary>
    internal sealed class ObjectParser : ItemParser
    {
        public static JsonValue Parse(object value)
        {
            ObjectParser parser = new ObjectParser(value);
            return parser.ParseRoot();
        }

        private ObjectParser(object value)
            : base(new ObjectItemizer(value))
        {
        }
    }
}
