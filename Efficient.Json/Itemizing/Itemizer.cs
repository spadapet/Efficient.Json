using Efficient.Json.Tokens;
using Efficient.Json.Utility;

namespace Efficient.Json.Itemizing
{
    /// <summary>
    /// Creates JSON one item at a time (can be from tokens, parsed values, or CLR objects)
    /// </summary>
    internal abstract class Itemizer : RecursiveBase
    {
        protected Itemizer()
        {
        }

        public abstract Item NextItem();
        public abstract FullToken ValueToken { get; }
    }
}
