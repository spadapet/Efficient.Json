using Efficient.Json.Path;

namespace Efficient.Json
{
    /// <summary>
    /// Info about a parsed JSON document
    /// </summary>
    internal class JsonContext
    {
        public string Text { get; set; } // Can be null during a streaming parse
        public JsonValue InvalidValue { get; set; }

        public JsonContext(string text)
        {
            this.Text = text;
        }

        public Query GetQuery(string path)
        {
            return new Query(path);
        }
    }
}
