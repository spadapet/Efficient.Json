using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Efficient.Json.Tests
{
    public class DeserializePerfTests
    {
        private readonly ITestOutputHelper output;

        public DeserializePerfTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private static string LargeFileName
        {
            get
            {
                string root = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return System.IO.Path.Combine(root, "Assets", "baby-names.json");
            }
        }

        [Fact]
        public void EfficientStream()
        {
            Stopwatch timer = Stopwatch.StartNew();
            long parseTime = 0;
            long convertTime = 0;

            for (int i = 0; i < 4; i++)
            {
                using (StreamReader reader = new StreamReader(DeserializePerfTests.LargeFileName, detectEncodingFromByteOrderMarks: true))
                {
                    Stopwatch innerTimer = Stopwatch.StartNew();
                    JsonValue value = JsonValue.StringToValue(reader);
                    parseTime += innerTimer.ElapsedMilliseconds;
                    innerTimer.Restart();

                    BabyNames names = value.ToObject<BabyNames>();
                    convertTime += innerTimer.ElapsedMilliseconds;

                    GC.KeepAlive(value);
                    GC.KeepAlive(names);
                }

            }

            long totalTime = timer.ElapsedMilliseconds;
            this.output.WriteLine($"Total time: {totalTime / 1000.0} seconds");
            this.output.WriteLine($"Parse time: {parseTime / 1000.0} seconds");
            this.output.WriteLine($"Convert time: {convertTime / 1000.0} seconds");
        }

        [Fact]
        public void EfficientText()
        {
            string json = File.ReadAllText(DeserializePerfTests.LargeFileName);
            Stopwatch timer = Stopwatch.StartNew();
            long parseTime = 0;
            long convertTime = 0;

            for (int i = 0; i < 4; i++)
            {
                Stopwatch innerTimer = Stopwatch.StartNew();
                JsonValue value = JsonValue.StringToValue(json);
                parseTime += innerTimer.ElapsedMilliseconds;
                innerTimer.Restart();

                BabyNames names = value.ToObject<BabyNames>();
                convertTime += innerTimer.ElapsedMilliseconds;

                GC.KeepAlive(value);
                GC.KeepAlive(names);
            }

            long totalTime = timer.ElapsedMilliseconds;
            this.output.WriteLine($"Total time: {totalTime / 1000.0} seconds");
            this.output.WriteLine($"Parse time: {parseTime / 1000.0} seconds");
            this.output.WriteLine($"Convert time: {convertTime / 1000.0} seconds");
        }

        [Fact]
        public void NewtonsoftStream()
        {
            Stopwatch timer = Stopwatch.StartNew();
            long parseTime = 0;
            long convertTime = 0;

            for (int i = 0; i < 4; i++)
            {
                using (StreamReader reader = new StreamReader(DeserializePerfTests.LargeFileName, detectEncodingFromByteOrderMarks: true))
                using (JsonReader jsonReader = new JsonTextReader(reader))
                {
                    Stopwatch innerTimer = Stopwatch.StartNew();
                    JObject value = JObject.Load(jsonReader);
                    parseTime += innerTimer.ElapsedMilliseconds;
                    innerTimer.Restart();

                    BabyNames names = value.ToObject<BabyNames>();
                    convertTime += innerTimer.ElapsedMilliseconds;

                    GC.KeepAlive(value);
                    GC.KeepAlive(names);
                }
            }

            long totalTime = timer.ElapsedMilliseconds;
            this.output.WriteLine($"Total time: {totalTime / 1000.0} seconds");
            this.output.WriteLine($"Parse time: {parseTime / 1000.0} seconds");
            this.output.WriteLine($"Convert time: {convertTime / 1000.0} seconds");
        }

        [Fact]
        public void NewtonsoftText()
        {
            string json = File.ReadAllText(DeserializePerfTests.LargeFileName);
            Stopwatch timer = Stopwatch.StartNew();
            long parseTime = 0;
            long convertTime = 0;

            for (int i = 0; i < 4; i++)
            {
                Stopwatch innerTimer = Stopwatch.StartNew();
                JObject value = JObject.Parse(json);
                parseTime += innerTimer.ElapsedMilliseconds;
                innerTimer.Restart();

                BabyNames names = value.ToObject<BabyNames>();
                convertTime += innerTimer.ElapsedMilliseconds;

                GC.KeepAlive(value);
                GC.KeepAlive(names);
            }

            long totalTime = timer.ElapsedMilliseconds;
            this.output.WriteLine($"Total time: {totalTime / 1000.0} seconds");
            this.output.WriteLine($"Parse time: {parseTime / 1000.0} seconds");
            this.output.WriteLine($"Convert time: {convertTime / 1000.0} seconds");
        }

        [Fact]
        public void EfficientDirectDeserializeStream()
        {
            long oldTime = 0;
            Stopwatch timer = Stopwatch.StartNew();

            for (int i = 0; i < 4; i++)
            {
                using (StreamReader reader = new StreamReader(DeserializePerfTests.LargeFileName, detectEncodingFromByteOrderMarks: true))
                {
                    BabyNames names = JsonValue.StringToObject<BabyNames>(reader);
                    GC.KeepAlive(names);
                }

                long newTime = timer.ElapsedMilliseconds;
                this.output.WriteLine($"Iteration {i + 1}: {(newTime - oldTime) / 1000.0} seconds");
                oldTime = newTime;
            }

            long totalTime = timer.ElapsedMilliseconds;
            this.output.WriteLine($"Total time: {totalTime / 1000.0} seconds");
        }

        [Fact]
        public void NewtonsoftDirectDeserializeStream()
        {
            long oldTime = 0;
            Stopwatch timer = Stopwatch.StartNew();
            JsonSerializer serializer = JsonSerializer.Create();

            for (int i = 0; i < 4; i++)
            {
                using (StreamReader reader = new StreamReader(DeserializePerfTests.LargeFileName, detectEncodingFromByteOrderMarks: true))
                using (JsonTextReader jsonReader = new JsonTextReader(reader))
                {
                    BabyNames names = serializer.Deserialize<BabyNames>(jsonReader);
                    GC.KeepAlive(names);
                }

                long newTime = timer.ElapsedMilliseconds;
                this.output.WriteLine($"Iteration {i + 1}: {(newTime - oldTime) / 1000.0} seconds");
                oldTime = newTime;
            }

            long totalTime = timer.ElapsedMilliseconds;
            this.output.WriteLine($"Total time: {totalTime / 1000.0} seconds");
        }

#pragma warning disable CS0649

        private class BabyNames
        {
            public BabyNamesMeta meta;
            public object[][] data;
        }

        private class BabyNamesMeta
        {
            public BabyNamesView view;
        }

        private class BabyNamesView
        {
            public string id;
            public string name;
            public string attribution;
            public int averageRating;
            public string category;
            public decimal createdAt;
            public string description;
            public string displayType;
            public int downloadCount;
            public bool hideFromCatalog;
            public bool hideFromDataJson;
            public int indexUpdatedAt;
            public bool newBackend;
            public int numberOfComments;
            public int oid;
            public string provenance;
            public bool publicationAppendEnabled;
            public int publicationDate;
            public int publicationGroup;
            public string publicationStage;
            public string rowClass;
            public int rowsUpdatedAt;
            public string rowsUpdatedBy;
            public int tableId;
            public int totalTimesRated;
            public int viewCount;
            public int viewLastModified;
            public string viewType;
            public BabyNameColumn[] columns;
            public BabyNameGrant[] grants;
            // public BabyNameMetadata metadata;
            public BabyNameOwner owner;
            public BabyNameQuery query;
            public string[] rights;
            public BabyNameOwner tableAuthor;
            public string[] tags;
            public string[] flags;
        }

        private class BabyNameColumn
        {
            public int id;
            public string name;
            public string dataTypeName;
            public string fieldName;
            public int position;
            public string renderTypeName;
            //public object format;
            public string[] flags;
        }

        private class BabyNameGrant
        {
            public bool inherited;
            public string type;
            public string[] flags;
        }

        private class BabyNameOwner
        {
            public string id;
            public string displayName;
            public string profileImageUrlLarge;
            public string profileImageUrlMedium;
            public string profileImageUrlSmall;
            public string screenName;
            public string type;
            public string[] flags;
        }

        private class BabyNameQuery
        {
            public BabyNameQueryOrderBy[] orderBys;
        }

        private class BabyNameQueryOrderBy
        {
            public bool ascending;
            public BabyNameQueryOrderByExpression expression;
        }

        private class BabyNameQueryOrderByExpression
        {
            public int columnId;
            public string type;
        }

        private struct BabyNamesRow
        {
            public object[] row;
        }

#pragma warning restore CS0649
    }
}
