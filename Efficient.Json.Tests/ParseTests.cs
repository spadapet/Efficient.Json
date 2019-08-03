using Efficient.Json.Tests.Utility;
using Xunit;

namespace Efficient.Json.Tests
{
    public class ParseTests
    {
        [Fact]
        public void TokenException()
        {
            JsonException ex = Assert.Throws<JsonException>(() => ParseUtility.ParseAndValidate(@"{ foo: bar }"));
            Assert.True(ex.HasToken);
            Assert.Equal(0, ex.TokenLine);
            Assert.Equal(2, ex.TokenColumn);
            Assert.Equal("foo", ex.TokenText.Substring(0, 3));
        }

        [Fact]
        public void ParseException()
        {
            JsonException ex = Assert.Throws<JsonException>(() => ParseUtility.ParseAndValidate(@"{ ""foo"":: bar }"));
            Assert.True(ex.HasToken);
            Assert.Equal(0, ex.TokenLine);
            Assert.Equal(8, ex.TokenColumn);
            Assert.Equal(":", ex.TokenText);
        }

        [Fact]
        public void ExceptionArray()
        {
            JsonException ex = Assert.Throws<JsonException>(() => ParseUtility.ParseAndValidate(@"{ ""foo"": [ 1, 2, { ""3"": { ""array"": [[,,]] } ] } }"));
            Assert.True(ex.HasToken);
            Assert.Equal(0, ex.TokenLine);
            Assert.Equal(37, ex.TokenColumn);
            Assert.Equal(",", ex.TokenText);
        }

        [Fact]
        public void Comment()
        {
            JsonValue value = ParseUtility.ParseAndValidate(
@"{
/*  Comment
    Comment
    Comment */
    ""foo"":  1,
// Comment
// Comment
    ""bar"": 2
}");

            Assert.Equal(1, value["foo"].Number);
            Assert.Equal(2, value["bar"].Number);
        }

        [Fact]
        public void LineColumn()
        {
            JsonException ex = Assert.Throws<JsonException>(() => ParseUtility.ParseAndValidate(
@"{
/*  Comment
    Comment
    Comment */
    ""foo"":  1,
// Comment
// Comment
    ""bar"": zoo
}"));

            Assert.True(ex.HasToken);
            Assert.Equal(7, ex.TokenLine);
            Assert.Equal(11, ex.TokenColumn);
            Assert.Equal("zoo", ex.TokenText.Substring(0, 3));
        }

        [Fact]
        public void EmptyContainers()
        {
            JsonValue value = ParseUtility.ParseAndValidate(@"{ ""dict"": { ""array"": [{}] }, ""array"": [ [], {}, [], {} ] }");
            Assert.Equal(1, value["dict"].Object.Count);
            Assert.Equal(4, value["array"].Array.Count);
        }
    }
}
