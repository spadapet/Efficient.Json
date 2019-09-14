using System.IO;
using Xunit;

namespace Efficient.Json.Tests.Utility
{
    internal static class ParseUtility
    {
        public static JsonValue ParseAndValidate(string text)
        {
            JsonValue value = JsonValue.StringToValue(text);
            return ParseUtility.ValidateParse(value);
        }

        public static JsonValue ParseAndValidate(TextReader reader)
        {
            JsonValue value = JsonValue.StringToValue(reader);
            return ParseUtility.ValidateParse(value);
        }

        public static string SerializeAndValidate(object value, bool formatted = false)
        {
            string text = JsonValue.ObjectToString(value, formatted);
            object value2 = JsonValue.StringToObject(text, value?.GetType() ?? typeof(object));
            string text2 = JsonValue.ObjectToString(value2, formatted);
            Assert.Equal(text, text2);

            return text;
        }

        private static JsonValue ValidateParse(JsonValue value)
        {
            for (int i = 0; i < 2; i++)
            {
                string newText1 = value.ToString(formatted: i == 1);
                JsonValue newValue = JsonValue.StringToValue(newText1);

                string newText2 = newValue.ToString(formatted: i == 1);
                Assert.Equal(newText1, newText2);
            }

            return value;
        }
    }
}
