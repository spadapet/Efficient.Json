using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Efficient.Json.Tests.Utility;
using Efficient.Json.Utility;
using Xunit;

namespace Efficient.Json.Tests
{
    public class DeserializeTests
    {
        [Fact]
        public void ToDateSuccess()
        {
            DateTime date = new DateTime(1970, 7, 4, 12, 0, 30, 500, DateTimeKind.Local);
            dynamic value = ParseUtility.ParseAndValidate($@"{{ ""date"": ""{date.ToString("O", CultureInfo.InvariantCulture)}"" }}");
            DateTime parsedDate = value.date;

            Assert.Equal(date, parsedDate);
        }

        [Fact]
        public void ToDateFailed()
        {
            DateTime date = new DateTime(1970, 7, 4, 12, 0, 30, 500, DateTimeKind.Local);
            dynamic value = ParseUtility.ParseAndValidate($@"{{ ""date"": ""Foo{date.ToString("O", CultureInfo.InvariantCulture)}"" }}");
            JsonException ex = Assert.Throws<JsonException>(() => (DateTime)value.date);
            Assert.IsType<FormatException>(ex.InnerException);
        }

        [Fact]
        public void ConvertDictToObject()
        {
            List<object> person = JsonValue.StringToObject<object>(@"{ ""name"": ""Person Last"", ""age"": 32 }") as List<object>;
            Assert.NotNull(person);

            IEnumerable<object> expected = new object[] { KeyValuePair.Create<string, object>("name", "Person Last"), KeyValuePair.Create<string, object>("age", (decimal)32) };
            Assert.Equal<IEnumerable<object>>(expected, person);
        }

        [Fact]
        public void ConvertArrayToObject()
        {
            string text = @"[""first"", 2, null, true]";
            List<object> stuff = JsonValue.StringToObject<object>(text) as List<object>;
            Assert.NotNull(stuff);

            IEnumerable<object> expected = new object[] { "first", (decimal)2, null, true };
            Assert.Equal(expected, stuff);
        }

        [Fact]
        public void ConvertArrayToKeyValuePairs()
        {
            string text = @"[""first"", 2, null, true]";
            List<KeyValuePair<string, object>> stuff = JsonValue.StringToObject<List<KeyValuePair<string, object>>>(text);
            Assert.NotNull(stuff);

            IEnumerable<KeyValuePair<string, object>> expected = new KeyValuePair<string, object>[]
            {
                KeyValuePair.Create<string, object>("0", "first"),
                KeyValuePair.Create<string, object>("1", (decimal)2),
                KeyValuePair.Create<string, object>("2", null),
                KeyValuePair.Create<string, object>("3", true),
            };

            Assert.Equal(expected, stuff);
        }

        [Fact]
        public void ConvertArrayToDict()
        {
            string text = @"[""first"", 2, null, true]";
            Dictionary<int, object> stuff = JsonValue.StringToObject<Dictionary<int, object>>(text);
            Assert.NotNull(stuff);

            IEnumerable<KeyValuePair<int, object>> expected = new KeyValuePair<int, object>[]
            {
                KeyValuePair.Create<int, object>(0, "first"),
                KeyValuePair.Create<int, object>(1, (decimal)2),
                KeyValuePair.Create<int, object>(2, null),
                KeyValuePair.Create<int, object>(3, true),
            };

            Assert.Equal(expected, stuff);
        }

        [Fact]
        public void ConvertSimpleStruct()
        {
            Person person = JsonValue.StringToObject<Person>(@"{ ""name"": ""Person Last"", ""born"": ""1/2/1979"", ""Tag"": ""tagged"" }");
            Assert.Equal(new Person("Person Last", DateTime.Parse("1/2/1979")), person);
            Assert.Equal("tagged", person.Tag);
        }

        [Fact]
        public void ConvertSimpleNested()
        {
            Cult cult = JsonValue.StringToObject<Cult>(
@"{
    'Name': 'Amazing Test Cult',
    'Active': true,
    'Leader': { 'name': 'Cult Leader', 'born': '7/4/1972' }
}".Replace('\'', '\"'));

            Assert.True(cult.Deserialized);
            Assert.Equal("Amazing Test Cult", cult.Name);
            Assert.True(cult.Active);
            Assert.Equal(0, cult.Followers.Count);
            Assert.Equal(new Person("Cult Leader", DateTime.Parse("7/4/1972")), cult.Leader);
        }

        [Fact]
        public void ConvertInterface()
        {
            CultWrapper cultWrapper = JsonValue.StringToObject<CultWrapper>(
@"{ 'Cult': {
    'Name': 'Amazing Test Cult',
    'Active': true,
    'Leader': { 'name': 'Cult Leader', 'born': '7/4/1972' }
} }".Replace('\'', '\"'));

            Assert.Equal("Amazing Test Cult", cultWrapper.Cult.Name);
            Assert.True(cultWrapper.Cult.Active);
            Assert.Equal(new Person("Cult Leader", DateTime.Parse("7/4/1972")), cultWrapper.Cult.Leader);
        }

        [Fact]
        public void ConvertDictionary()
        {
            Dictionary<string, DateTime> dict = JsonValue.StringToObject<Dictionary<string, DateTime>>(@"{ ""born1"": ""1/2/1979"", ""born2"": ""3/4/1981"" }");
            Assert.Equal(DateTime.Parse("1/2/1979"), dict["born1"]);
            Assert.Equal(DateTime.Parse("3/4/1981"), dict["born2"]);
        }

        [Fact]
        public void ConvertIDictionary()
        {
            IDictionary dict = JsonValue.StringToObject<IDictionary>(@"{ ""born1"": ""1/2/1979"", ""born2"": ""3/4/1981"" }");
            Assert.Equal("1/2/1979", dict["born1"]);
            Assert.Equal("3/4/1981", dict["born2"]);
        }

        [Fact]
        public void ConvertIDictionaryT()
        {
            IDictionary<string, DateTime> dict = JsonValue.StringToObject<IDictionary<string, DateTime>>(@"{ ""born1"": ""1/2/1979"", ""born2"": ""3/4/1981"" }");
            Assert.Equal(DateTime.Parse("1/2/1979"), dict["born1"]);
            Assert.Equal(DateTime.Parse("3/4/1981"), dict["born2"]);
        }

        [Fact]
        public void ConvertArrayIntermediate1()
        {
            JsonValue value = ParseUtility.ParseAndValidate(@"[ 1, 2, 3, 4 ]");
            int[] ints = value.ToObject<int[]>();
            object[] objects = value.ToObject<object[]>();

            Assert.Equal(4, objects.Length);
            Assert.Equal(4, ints.Length);
            Assert.Equal(new int[] { 1, 2, 3, 4 }, ints);
        }

        [Fact]
        public void ConvertArrayIntermediate2()
        {
            JsonValue value = ParseUtility.ParseAndValidate(@"[ [ 2, 4, 6, 8 ], [ 1, 3, 5 ] ]");
            int[][] ints = value.ToObject<int[][]>();

            Assert.Equal(2, ints.Length);
            Assert.Equal(new int[][] { new int[] { 2, 4, 6, 8 }, new int[] { 1, 3, 5 } }, ints);
        }

        [Fact]
        public void ConvertArrayDirect1()
        {
            const string json = @"[ 1, 2, 3, 4 ]";
            int[] ints = JsonValue.StringToObject<int[]>(json);
            object[] objects = JsonValue.StringToObject<object[]>(json);
            IList<double> ilistDoubles = JsonValue.StringToObject<IList<double>>(json);
            List<string> listStrings = JsonValue.StringToObject<List<string>>(json);

            Assert.Equal(4, ints.Length);
            Assert.Equal(4, objects.Length);
            Assert.Equal(4, ilistDoubles.Count);
            Assert.Equal(4, listStrings.Count);
            Assert.Equal(new int[] { 1, 2, 3, 4 }, ints);
            Assert.Equal(new object[] { (decimal)1, (decimal)2, (decimal)3, (decimal)4 }, objects);
            Assert.Equal(new double[] { 1, 2, 3, 4 }, ilistDoubles);
            Assert.Equal(new string[] { "1", "2", "3", "4" }, listStrings);
        }

        [Fact]
        public void ConvertArrayDirect2()
        {
            int[][] ints = JsonValue.StringToObject<int[][]>(@"[ [ 2, 4, 6, 8 ], [ 1, 3, 5 ] ]");

            Assert.Equal(2, ints.Length);
            Assert.Equal(new int[][] { new int[] { 2, 4, 6, 8 }, new int[] { 1, 3, 5 } }, ints);
        }

        [Fact]
        public void ConvertToNullable()
        {
            Cult cult1 = JsonValue.StringToObject<Cult>(@"{ ""EndDate"": ""1/2/1979"" }");
            Cult cult2 = JsonValue.StringToObject<Cult>(@"{ ""EndDate"": null }");
            Cult cult3 = JsonValue.StringToObject<Cult>(@"{ }");
            Cult cult4 = JsonValue.StringToObject<Cult>(@"{ ""EndDate"": ""1/2/1979"", ""EndDate"": ""2/1/1979"" }");
            Cult cult5 = JsonValue.StringToObject<Cult>(@"{ ""EndDate"": null, ""EndDate"": ""2/1/1979"" }");

            Assert.True(cult1.EndDate.HasValue);
            Assert.False(cult2.EndDate.HasValue);
            Assert.False(cult3.EndDate.HasValue);
            Assert.True(cult4.EndDate.HasValue);
            Assert.True(cult5.EndDate.HasValue);
            Assert.Equal(DateTime.Parse("1/2/1979"), cult1.EndDate.Value);
            Assert.Equal(DateTime.Parse("2/1/1979"), cult4.EndDate.Value);
            Assert.Equal(DateTime.Parse("2/1/1979"), cult5.EndDate.Value);
        }

        [Fact]
        public void ConvertToArrayOfNullable()
        {
            TestStuff stuff = JsonValue.StringToObject<TestStuff>(@"{ ""NullableIntsField"": [ 1, 2, 3, 4 ], ""NullableIntsProperty"": [ 5, 6, 7, 8 ] }");

            Assert.NotNull(stuff.NullableIntsField);
            Assert.NotNull(stuff.NullableIntsProperty);
            Assert.Equal(new int?[] { 1, 2, 3, 4 }, stuff.NullableIntsField);
            Assert.Equal(new int?[] { 5, 6, 7, 8 }, stuff.NullableIntsProperty);
        }

        [Fact]
        public void ConvertToCult()
        {
            Cult cult = JsonValue.StringToObject<Cult>(
@"{
    'Name': 'Amazing Test Cult',
    'Active': true,
    'Leader': { 'name': 'Cult Leader', 'born': '7/4/1972' },
    'Followers':
    [
        { 'name': 'Follower 1', 'born': '1/2/1979' },
        { 'name': 'Follower 2', 'born': '3/4/1980' },
        { 'name': 'Follower 3', 'born': '5/6/1981' }
    ]
}".Replace('\'', '\"'));

            Assert.True(cult.Deserialized);
            Assert.Equal("Amazing Test Cult", cult.Name);
            Assert.True(cult.Active);
            Assert.Equal(3, cult.Followers.Count);

            Assert.Equal(new Person("Cult Leader", DateTime.Parse("7/4/1972")), cult.Leader);
            Assert.Equal(new Person("Follower 1", DateTime.Parse("1/2/1979")), cult.Followers[0]);
            Assert.Equal(new Person("Follower 2", DateTime.Parse("3/4/1980")), cult.Followers[1]);
            Assert.Equal(new Person("Follower 3", DateTime.Parse("5/6/1981")), cult.Followers[2]);
        }

#pragma warning disable CS0649

        private struct Person
        {
            public string name;
            public DateTime born;
            public string Tag { get; set; }

            public Person(string name, DateTime born)
            {
                this.name = name;
                this.born = born;
                this.Tag = string.Empty;
            }

            public override bool Equals(object obj)
            {
                return obj is Person other && this.name == other.name && this.born == other.born;
            }

            public override int GetHashCode()
            {
                return HashUtility.CombineHashCodes(this.name.GetHashCode(), this.born.GetHashCode());
            }
        }

        private interface ICult
        {
            Person Leader { get; set; }
            IList<Person> Followers { get; }
            bool Active { get; set; }
            string Name { get; set; }
        }

        [DataContract]
        private class Cult : ICult
        {
            [DataMember] public Person Leader { get; set; }
            [DataMember] public IList<Person> Followers { get; } = new List<Person>();
            [DataMember] public bool Active { get; set; }
            [DataMember] public string Name { get; set; }
            [DataMember] public DateTime? EndDate { get; set; }
            public bool Deserialized { get; set; }

            [OnDeserializing]
            public void OnDeserialized(StreamingContext context)
            {
                this.Deserialized = true;
            }
        }

        private class CultWrapper
        {
            public ICult Cult { get; } = new Cult();
        }

        private class TestStuff
        {
            public int?[] NullableIntsField;
            public int?[] NullableIntsProperty { get; set; }
        }

#pragma warning restore CS0649
    }
}
