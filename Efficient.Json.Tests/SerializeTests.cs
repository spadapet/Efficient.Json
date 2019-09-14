using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Efficient.Json.Tests.Utility;
using Efficient.Json.Utility;
using Xunit;

namespace Efficient.Json.Tests
{
    public class SerializeTests
    {
        [Fact]
        public void WritePrimitives()
        {
            Assert.Equal("null", ParseUtility.SerializeAndValidate(null, formatted: false));
            Assert.Equal("true", ParseUtility.SerializeAndValidate(true, formatted: false));
            Assert.Equal("false", ParseUtility.SerializeAndValidate(false, formatted: false));
            Assert.Equal("\"hello world\"", ParseUtility.SerializeAndValidate("hello world", formatted: false));
            Assert.Equal("12.34", ParseUtility.SerializeAndValidate(decimal.Parse("12.34"), formatted: false));
            Assert.Equal("[1,2,3,4]", ParseUtility.SerializeAndValidate(new uint[] { 1, 2, 3, 4 }, formatted: false));
            Assert.Equal("[1,\"2\",3,null,\"01/02/2003 00:00:00\"]", ParseUtility.SerializeAndValidate(new object[] { 1, "2", 3.0, null, DateTime.Parse("1/2/2003") }, formatted: false));
        }

        [Fact]
        public void WriteDictionary()
        {
            Dictionary<string, int> dict1 = new Dictionary<string, int>()
            {
                { "Foo1", 1 },
                { "Foo2", 2 },
                { "Foo3", 3 },
                { "Foo4", 4 },
            };

            Dictionary<int, string> dict2 = new Dictionary<int, string>()
            {
                { 1, "Foo1" },
                { 2, "Foo2" },
                { 3, "Foo3" },
                { 4, "Foo4" },
            };

            Dictionary<object, object> dict3 = new Dictionary<object, object>()
            {
                { 1, "Foo1" },
                { "Foo2", true },
                { 3.0, null },
                { false, "Foo4" },
                { DateTime.Parse("1/2/2003"), decimal.Parse("12.34") },
            };

            const string text1 = @"{""Foo1"":1,""Foo2"":2,""Foo3"":3,""Foo4"":4}";
            const string text2 = @"{""1"":""Foo1"",""2"":""Foo2"",""3"":""Foo3"",""4"":""Foo4""}";
            const string text3 = @"{""1"":""Foo1"",""Foo2"":true,""3"":null,""false"":""Foo4"",""01/02/2003 00:00:00"":12.34}";

            Assert.Equal(text1, ParseUtility.SerializeAndValidate(dict1, formatted: false));
            Assert.Equal(text2, ParseUtility.SerializeAndValidate(dict2, formatted: false));
            Assert.Equal(text3, ParseUtility.SerializeAndValidate(dict3, formatted: false));

            Assert.Equal(text1, ParseUtility.SerializeAndValidate(dict1.ToList(), formatted: false));
            Assert.Equal(text2, ParseUtility.SerializeAndValidate(dict2.ToList(), formatted: false));
            Assert.Equal(text3, ParseUtility.SerializeAndValidate(dict3.ToList(), formatted: false));

            Assert.Equal(text1, ParseUtility.SerializeAndValidate(dict1.ToArray(), formatted: false));
            Assert.Equal(text2, ParseUtility.SerializeAndValidate(dict2.ToArray(), formatted: false));
            Assert.Equal(text3, ParseUtility.SerializeAndValidate(dict3.ToArray(), formatted: false));
        }

        [Fact]
        public void WriteCult()
        {
            Cult cult = new Cult()
            {
                Active = true,
                EndDate = DateTime.Parse("10/11/2012"),
                Leader = new Person("Leader Person", DateTime.Parse("1/2/2003")),
                Name = "Funny cult",
            };

            cult.Followers.Add(new Person("Person 1", DateTime.Parse("2/1/2002")));
            cult.Followers.Add(new Person("Person 2", DateTime.Parse("4/3/2003")));

            string text = JsonValue.ObjectToString(cult, formatted: false);
            Assert.Equal(@"{""Name"":""Funny cult"",""Active"":true,""EndDate"":""10/11/2012 00:00:00"",""Leader"":{""born"":""01/02/2003 00:00:00"",""name"":""Leader Person""},""Followers"":[{""born"":""02/01/2002 00:00:00"",""name"":""Person 1""},{""born"":""04/03/2003 00:00:00"",""name"":""Person 2""}]}", text);
        }

#pragma warning disable CS0649

        private struct Person
        {
            public string name;
            public DateTime born;

            public Person(string name, DateTime born)
            {
                this.name = name;
                this.born = born;
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

        [DataContract]
        private class CultBase
        {
            [DataMember(Order = 100, EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Order = 200, EmitDefaultValue = false)] public bool Active { get; set; }
            public bool Serialized { get; private set; }

            [OnSerializing]
            public void OnSerialized(StreamingContext context)
            {
                this.Serialized = true;
            }
        }

        [DataContract]
        private class Cult : CultBase
        {
            [DataMember(Order = 10, EmitDefaultValue = false)] public Person Leader { get; set; }
            [DataMember(Order = 20)] public IList<Person> Followers { get; } = new List<Person>();
            [DataMember] public DateTime? EndDate { get; set; }
        }

        private class TestStuff
        {
            public int?[] NullableIntsField;
            public int?[] NullableIntsProperty { get; set; }
        }

#pragma warning restore CS0649
    }
}
