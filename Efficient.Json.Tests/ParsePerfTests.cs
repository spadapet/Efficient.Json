using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Efficient.Json.Tests
{
    public class ParsePerfTests
    {
        private readonly ITestOutputHelper output;

        public ParsePerfTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private static string LargeFileName
        {
            get
            {
                string root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(root, "Assets", "baby-names.json");
            }
        }

        private void FileTestPerf<S, T>(string fileName, Func<string, S> openFileFunc, Func<S, T> parseFunc, Func<T, int> deepHashFunc)
        {
            S fileContents = openFileFunc(fileName);
            try
            {
                long memoryBeforeParse = GC.GetTotalMemory(true);
                Stopwatch timer = Stopwatch.StartNew();

                // Parse the whole JSON file
                T value = parseFunc(fileContents);
                long parseMilliseconds = timer.ElapsedMilliseconds;

                long memoryAfterParse = GC.GetTotalMemory(true);
                timer.Restart();

                // Iterate through every value in the JSON
                _ = deepHashFunc(value);

                long hashMilliseconds = timer.ElapsedMilliseconds;
                long memoryAfterHash = GC.GetTotalMemory(true);

                this.output.WriteLine($"TEST {typeof(T).FullName} parse {typeof(S).FullName}:");
                this.output.WriteLine($"* Parse time:   {parseMilliseconds / 1000.0} seconds");
                this.output.WriteLine($"* Iterate time: {hashMilliseconds / 1000.0} seconds");
                this.output.WriteLine($"* Parse memory used:   {(memoryAfterParse - memoryBeforeParse) / 1000000.0} MB");
                this.output.WriteLine($"* Iterate memory used: {(memoryAfterHash - memoryAfterParse) / 1000000.0} MB");

                GC.KeepAlive(value);
            }
            finally
            {
                (fileContents as IDisposable)?.Dispose();
            }
        }

        private void ReadFileTestPerf<T>(string fileName, Func<string, T> parseFunc, Func<T, int> deepHashFunc)
        {
            this.FileTestPerf(fileName, File.ReadAllText, parseFunc, deepHashFunc);
        }

        private void StreamFileTestPerf<T>(string fileName, Func<Stream, T> parseFunc, Func<T, int> deepHashFunc)
        {
            this.FileTestPerf(fileName, File.OpenRead, parseFunc, deepHashFunc);
        }

        private static int GetDeepHashCode(JsonValue value)
        {
            int hash = 0;

            if (value.IsArray)
            {
                foreach (JsonValue child in value.Array)
                {
                    hash ^= ParsePerfTests.GetDeepHashCode(child);
                }
            }
            else if (value.IsObject)
            {
                foreach (KeyValuePair<string, JsonValue> pair in value.Object)
                {
                    hash ^= ParsePerfTests.GetDeepHashCode(pair.Value);
                }
            }
            else if (!value.IsNull)
            {
                hash = value.Value.GetHashCode();
            }

            return hash;
        }

        private static int GetDeepHashCode(JToken value)
        {
            int hash = 0;

            if (value.Type == JTokenType.Array)
            {
                foreach (JToken child in (JArray)value)
                {
                    hash ^= ParsePerfTests.GetDeepHashCode(child);
                }
            }
            else if (value.Type == JTokenType.Object)
            {
                foreach (KeyValuePair<string, JToken> pair in (JObject)value)
                {
                    hash ^= ParsePerfTests.GetDeepHashCode(pair.Value);
                }
            }
            else if (value.Type != JTokenType.Null)
            {
                hash = value.Value<object>().GetHashCode();
            }

            return hash;
        }

        private static int GetDeepHashCode(System.Json.JsonValue value)
        {
            int hash = 0;

            if (value != null)
            {
                if (value.JsonType == System.Json.JsonType.Array)
                {
                    foreach (System.Json.JsonValue child in (System.Json.JsonArray)value)
                    {
                        hash ^= ParsePerfTests.GetDeepHashCode(child);
                    }
                }
                else if (value.JsonType == System.Json.JsonType.Object)
                {
                    foreach (KeyValuePair<string, System.Json.JsonValue> pair in (System.Json.JsonObject)value)
                    {
                        hash ^= ParsePerfTests.GetDeepHashCode(pair.Value);
                    }
                }
                else if (value.JsonType == System.Json.JsonType.Boolean)
                {
                    hash = ((bool)value).GetHashCode();
                }
                else if (value.JsonType == System.Json.JsonType.String)
                {
                    hash = ((string)value).GetHashCode();
                }
                else if (value.JsonType == System.Json.JsonType.Number)
                {
                    hash = ((decimal)value).GetHashCode();
                }
            }

            return hash;
        }

#if NET_CORE_3
        private static int GetDeepHashCode(System.Text.Json.JsonElement value)
        {
            int hash = 0;

            if (value.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (System.Text.Json.JsonElement child in value.EnumerateArray())
                {
                    hash ^= JsonPerfTests.GetDeepHashCode(child);
                }
            }
            else if (value.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (System.Text.Json.JsonProperty pair in value.EnumerateObject())
                {
                    hash ^= JsonPerfTests.GetDeepHashCode(pair.Value);
                }
            }
            else if (value.ValueKind == System.Text.Json.JsonValueKind.False ||
                value.ValueKind == System.Text.Json.JsonValueKind.True)
            {
                hash = value.GetBoolean().GetHashCode();
            }
            else if (value.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                hash = value.GetString().GetHashCode();
            }
            else if (value.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                hash = value.GetDecimal().GetHashCode();
            }

            return hash;
        }
#endif

        [Fact]
        public void LargeFileEfficient()
        {
            this.ReadFileTestPerf(
                ParsePerfTests.LargeFileName,
                JsonValue.StringToValue,
                ParsePerfTests.GetDeepHashCode);
        }

        [Fact]
        public void LargeFileEfficientStringReader()
        {
            this.ReadFileTestPerf(
                ParsePerfTests.LargeFileName,
                s => JsonValue.StringToValue(new StringReader(s)),
                ParsePerfTests.GetDeepHashCode);
        }

        [Fact]
        public void LargeFileJsonNet()
        {
            this.ReadFileTestPerf(
                ParsePerfTests.LargeFileName,
                JObject.Parse,
                ParsePerfTests.GetDeepHashCode);
        }

        [Fact]
        public void LargeFileSystemJson()
        {
            this.ReadFileTestPerf(
                ParsePerfTests.LargeFileName,
                System.Json.JsonObject.Parse,
                ParsePerfTests.GetDeepHashCode);
        }

        [Fact]
        public void StreamLargeFileEfficient()
        {
            this.StreamFileTestPerf(
                ParsePerfTests.LargeFileName,
                s =>
                {
                    using (StreamReader reader = new StreamReader(s, detectEncodingFromByteOrderMarks: true))
                    {
                        return JsonValue.StringToValue(reader);
                    }
                },
                v => ParsePerfTests.GetDeepHashCode(v));
        }

        [Fact]
        public void StreamLargeFileJsonNet()
        {
            this.StreamFileTestPerf(
                ParsePerfTests.LargeFileName,
                s =>
                {
                    using (StreamReader streamReader = new StreamReader(s, detectEncodingFromByteOrderMarks: true))
                    using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
                    {
                        return JObject.Load(jsonReader);
                    }
                },
                ParsePerfTests.GetDeepHashCode);
        }

        [Fact]
        public void StreamLargeFileSystemJson()
        {
            this.StreamFileTestPerf(
                ParsePerfTests.LargeFileName,
                System.Json.JsonObject.Load,
                ParsePerfTests.GetDeepHashCode);
        }

#if NET_CORE_3
        [Fact]
        public void LargeFileSystemTextJson()
        {
            this.ReadFileTestPerf(
                JsonPerfTests.LargeFileName,
                s => System.Text.Json.JsonDocument.Parse(s),
                d => JsonPerfTests.GetDeepHashCode(d.RootElement));
        }

        [Fact]
        public void StreamLargeFileSystemTextJson()
        {
            this.StreamFileTestPerf(
                JsonPerfTests.LargeFileName,
                s => System.Text.Json.JsonDocument.Parse(s),
                d => JsonPerfTests.GetDeepHashCode(d.RootElement));
        }
#endif
    }
}
