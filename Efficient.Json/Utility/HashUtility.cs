namespace Efficient.Json.Utility
{
    internal static class HashUtility
    {
        public static int CombineHashCodes(int h1, int h2)
        {
            return ((h1 << 5) + h1) ^ h2;
        }

        public static int CombineHashCodes(int h1, int h2, int h3, int h4)
        {
            return HashUtility.CombineHashCodes(
                HashUtility.CombineHashCodes(h1, h2),
                HashUtility.CombineHashCodes(h3, h4));
        }
    }
}
