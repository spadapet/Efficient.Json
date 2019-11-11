using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Efficient.Json.Path
{
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Not implemented yet")]
    internal class Query
    {
        public Query(string path)
        {
        }

        public IEnumerable<JsonValue> Select(JsonValue root)
        {
            throw new NotImplementedException(Resources.Query_NotImplemented);
        }
    }
}
