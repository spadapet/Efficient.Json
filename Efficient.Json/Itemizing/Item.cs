using System.Diagnostics;

namespace Efficient.Json.Itemizing
{
    /// <summary>
    /// Output of the itemizer
    /// </summary>
    [DebuggerDisplay("{Type}")]
    internal struct Item
    {
        public readonly ItemType Type;
        public readonly int Size;
        public readonly object Data;

        public Item(ItemType type)
        {
            this.Type = type;
            this.Size = 0;
            this.Data = null;
        }

        public Item(ItemType type, int size)
        {
            this.Type = type;
            this.Size = size;
            this.Data = null;
        }

        public Item(ItemType type, object data)
        {
            this.Type = type;
            this.Size = 0;
            this.Data = data;
        }
    }
}
