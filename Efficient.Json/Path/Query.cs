using System.Collections.Generic;

namespace Efficient.Json.Path
{
    internal class Query
    {
        private string path;

        public Query(string path)
        {
            this.path = path;
        }

        public IEnumerable<JsonValue> Select(JsonValue root)
        {
            yield break;
        }
    }
}
