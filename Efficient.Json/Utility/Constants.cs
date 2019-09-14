namespace Efficient.Json.Utility
{
    internal static class Constants
    {
        public const int BufferSize = 32;

        public static object TrueObject = true;
        public static object FalseObject = false;

        public static string GetIndent(bool formatted) => formatted ? "    " : string.Empty;
    }
}
