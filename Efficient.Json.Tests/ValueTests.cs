using System.Collections.Generic;
using Efficient.Json.Tests.Utility;
using Xunit;

namespace Efficient.Json.Tests
{
    public class ValueTests
    {
        [Fact]
        public void DictionaryEnum()
        {
            JsonValue value = ParseUtility.ParseAndValidate(@"{ ""0"": 0, ""1"": 1, ""2"": 2, ""3"": 3, ""4"": 4 }");

            foreach (KeyValuePair<string, JsonValue> pair in value.Object)
            {
                Assert.Equal(int.Parse(pair.Key), pair.Value.Number);
            }

            foreach (string key in value.Object.Keys)
            {
                Assert.True(value.Object.ContainsKey(key));
                Assert.Equal(int.Parse(key), value[key].Number);
            }
        }

        [Fact]
        public void ArrayEnum()
        {
            JsonValue value = ParseUtility.ParseAndValidate(@"{ ""array"": [ 0, 1, 2, 3, 4 ] }");
            Assert.True(value.IsObject);

            JsonValue array = value["array"];
            Assert.True(array.IsArray);

            int i = 0;
            foreach (JsonValue child in array.Array)
            {
                Assert.Equal(i++, child.Number);
            }

            for (i = 0; i < array.Array.Count; i++)
            {
                Assert.Equal(i, array[i].Number);
            }

            Assert.False(array[i].IsValid);
        }

        [Fact]
        public void MissingLookup()
        {
            JsonValue value = ParseUtility.ParseAndValidate(@"{ ""foo"": ""bar"" }");

            JsonValue bar = value["bar"];
            Assert.True(!bar.IsValid);

            bar = bar["foo"];
            Assert.True(!bar.IsValid);

            bar = value[10];
            Assert.True(!bar.IsValid);

            bar = bar[10];
            Assert.True(!bar.IsValid);
        }

        [Fact]
        public void SimpleLookupTypes()
        {
            JsonValue value = ParseUtility.ParseAndValidate(
@"{
    ""string"": ""bar"",
    ""int"": 32,
    ""double"": 32.5,
    ""bool"": true,
    ""null"": null,
    ""array"": [ 0, 1, 2 ],
    ""dict"": { ""array"": [ 0, 1, 2 ] }
}");

            JsonValue stringValue = value["string"];
            JsonValue intValue = value["int"];
            JsonValue doubleValue = value["double"];
            JsonValue boolValue = value["bool"];
            JsonValue nullValue = value["null"];
            JsonValue arrayValue = value["array"];
            JsonValue dictValue = value["dict"];

            Assert.True(stringValue.IsString);
            Assert.True(intValue.IsNumber);
            Assert.Equal(32, intValue.Number, 2);
            Assert.True(doubleValue.IsNumber);
            Assert.Equal((decimal)32.5, doubleValue.Number, 2);
            Assert.True(boolValue.IsBool);
            Assert.True(nullValue.IsNull);
            Assert.True(arrayValue.IsArray);
            Assert.True(dictValue.IsObject);

            JsonValue nestedIntValue = value["array"][1];
            JsonValue nestedArrayValue = value["dict"]["array"];

            Assert.True(nestedIntValue.IsNumber);
            Assert.Equal(1, nestedIntValue.Number, 2);
            Assert.True(nestedArrayValue.IsArray);
            Assert.Equal(arrayValue.Array, nestedArrayValue.Array);
        }
    }
}
