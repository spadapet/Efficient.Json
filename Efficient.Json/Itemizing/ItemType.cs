namespace Efficient.Json.Itemizing
{
    internal enum ItemType
    {
        None,
        ArrayStart,
        ArrayEnd,
        ObjectStart,
        ObjectEnd,
        Key,
        Value,
        End,
    }
}
