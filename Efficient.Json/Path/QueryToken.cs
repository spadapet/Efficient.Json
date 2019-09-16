namespace Efficient.Json.Path
{
    internal struct QueryToken
    {
        public readonly QueryTokenType Type;
        public readonly int Start;
        public readonly int Length;

        public QueryToken(QueryTokenType type, int start, int length)
        {
            this.Type = type;
            this.Start = start;
            this.Length = length;
        }
    }
}
