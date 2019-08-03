using System;
using System.Collections.Generic;
using Efficient.Json.Tests.Utility;
using Xunit;

namespace Efficient.Json.Tests
{
    public class DynamicTests
    {
        [Fact]
        public void DictionaryEnum()
        {
            dynamic value = ParseUtility.ParseAndValidate(@"{ ""0"": 0, ""1"": 1, ""2"": 2, ""3"": 3, ""4"": 4 }");

            foreach (KeyValuePair<string, object> pair in value)
            {
                int i = (int)(decimal)pair.Value;
                Assert.Equal(i, int.Parse(pair.Key));
            }
        }

        [Fact]
        public void ArrayEnum()
        {
            dynamic value = ParseUtility.ParseAndValidate(@"{ ""array"": [ 0, 1, 2, 3, 4 ] }");
            dynamic darray = value.array;
            dynamic darray2 = value["array"];
            IEnumerable<int> iarray = darray;
            IEnumerable<int> iarray2 = darray2;
            Assert.Equal(iarray, iarray2);

            object[] array = darray;
            List<KeyValuePair<string, int>> array1 = value.array;
            List<KeyValuePair<int, int>> array2 = value.array;

            int i = 0;
            foreach (decimal child in array)
            {
                int h = (int)child;
                Assert.Equal(h, int.Parse(array1[i].Key));
                Assert.Equal(h, array1[i].Value);
                Assert.Equal(h, array2[i].Key);
                Assert.Equal(h, array2[i].Value);
                Assert.Equal(h, (int)darray[i]);
                Assert.Equal(h, i++);
            }

            for (i = 0; i < array.Length; i++)
            {
                int h = (int)(decimal)array[i];
                Assert.Equal(h, i);
            }
        }

        [Fact]
        public void MissingLookup()
        {
            dynamic value = ParseUtility.ParseAndValidate(@"{ ""foo"": ""bar"" }");
            Assert.Throws<KeyNotFoundException>(() => value.bar);
        }

        [Fact]
        public void MissingIndex()
        {
            dynamic value = ParseUtility.ParseAndValidate(@"{ ""foo"": [ 0, 1 ] }");
            Assert.Throws<IndexOutOfRangeException>(() => value.foo[10]);
        }

        [Fact]
        public void SimpleLookupTypes()
        {
            dynamic value = ParseUtility.ParseAndValidate(
@"{
    ""string"": ""bar"",
    ""int"": 32,
    ""double"": 32.5,
    ""bool"": true,
    ""null"": null,
    ""array"": [ 0, 1, 2 ],
    ""dict"": { ""array"": [ 0, 1, 2 ] }
}");

            string stringValue = value.@string;
            int intValue = value.@int;
            double doubleValue = value.@double;
            bool boolValue = value.@bool;
            object nullValue = value.@null;
            object[] arrayValue = value.array;
            IDictionary<string, object> dictValue = value.dict;

            Assert.Equal("bar", stringValue);
            Assert.Equal(32, intValue);
            Assert.Equal(32.5, doubleValue);
            Assert.True(boolValue);
            Assert.Null(nullValue);
            Assert.Equal(3, arrayValue.Length);
            Assert.Equal(1, dictValue.Count);
        }
    }
}
