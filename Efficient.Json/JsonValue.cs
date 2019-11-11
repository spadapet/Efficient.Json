using Efficient.Json.Itemizing;
using Efficient.Json.Parsing;
using Efficient.Json.Path;
using Efficient.Json.Serializing;
using Efficient.Json.Tokenizing;
using Efficient.Json.Tokens;
using Efficient.Json.Utility;
using Efficient.Json.Value;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Efficient.Json
{
    /// <summary>
    /// Represents anything in the parsed JSON tree. Static methods on here are used to begin parsing.
    /// </summary>
    [DebuggerTypeProxy(typeof(DebuggerView))]
    public class JsonValue : IReadOnlyList<JsonValue>, IReadOnlyDictionary<string, JsonValue>, IValueDynamic, IDynamicMetaObjectProvider
    {
        private readonly ValueData data;
        private object cachedValue;

        internal JsonValue(ValueData data)
        {
            this.data = data;
        }

        internal JsonValue(ValueData data, object cachedValue)
        {
            this.data = data;
            this.cachedValue = cachedValue;
        }

        private Token Token => this.data.Token;
        private ValueList List => this.data.List;
        private JsonContext Context => this.data.Context;

        internal TokenType Type => this.data.Token.Type;
        internal JsonValue[] InternalArray => this.List.Values;
        internal Key[] InternalKeys => this.List.Keys;
        internal FullToken FullToken => new FullToken(this.data.Token, this.Context.Text, 0, 0);

        public bool IsArray => this.Type == TokenType.ArrayValue;
        public bool IsBool => this.Type == TokenType.True || this.Type == TokenType.False;
        public bool IsObject => this.Type == TokenType.ObjectValue;
        public bool IsNumber => this.Type == TokenType.Number;
        public bool IsNull => this.Type == TokenType.Null;
        public bool IsString => this.Type == TokenType.String || this.Type == TokenType.EncodedString;
        public bool IsValid => this.Type != TokenType.None;

        public IReadOnlyList<JsonValue> Array => this;
        public bool Bool => (bool)this.BoolObject;
        public IReadOnlyDictionary<string, JsonValue> Object => this;
        public decimal Number => (decimal)this.NumberObject;
        public string String => (string)this.StringObject;
        public JsonValue this[int index] => this.List.Array[index];
        public JsonValue this[string key] => this.List.Dictionary[key];

        private object BoolObject
        {
            get
            {
                if (!this.IsBool)
                {
                    throw JsonException.WrongType(this.Type, "Boolean");
                }

                return (this.Type == TokenType.True)
                    ? Constants.TrueObject
                    : Constants.FalseObject;
            }
        }

        private object NumberObject
        {
            get
            {
                if (this.cachedValue is decimal)
                {
                    return this.cachedValue;
                }

                if (!this.IsNumber)
                {
                    throw JsonException.WrongType(this.Type, TokenType.Number);
                }

                string rawString = (this.cachedValue as string) ?? EncodingUtility.DecodeToken(this.Context.Text, this.Token).Text;
                if (!decimal.TryParse(rawString,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out decimal value))
                {
                    throw JsonException.New(Resources.Value_BadNumber, rawString);
                }

                return this.cachedValue = value;
            }
        }

        private object StringObject
        {
            get
            {
                if (this.cachedValue is string)
                {
                    return this.cachedValue;
                }

                if (!this.IsString)
                {
                    throw JsonException.WrongType(this.Type, TokenType.String);
                }

                return this.cachedValue = EncodingUtility.DecodeToken(this.Context.Text, this.Token).Text;
            }
        }

        public object Value
        {
            get
            {
                switch (this.Type)
                {
                    case TokenType.ArrayValue:
                        return this.List.Array;

                    case TokenType.True:
                    case TokenType.False:
                        return this.BoolObject;

                    case TokenType.ObjectValue:
                        return this.List.Dictionary;

                    case TokenType.Number:
                        return this.NumberObject;

                    case TokenType.String:
                    case TokenType.EncodedString:
                        return this.StringObject;

                    case TokenType.Null:
                        return null;

                    default:
                        throw JsonException.WrongType(this.Type, "Object");
                }
            }
        }

        ////////////////////////////////////////
        // Collection implementations

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerator enumerator;

            if (this.IsArray)
            {
                IEnumerable<JsonValue> enumerable = this;
                enumerator = enumerable.GetEnumerator();
            }
            else if (this.IsObject)
            {
                IEnumerable<KeyValuePair<string, JsonValue>> enumerable = this;
                enumerator = enumerable.GetEnumerator();
            }
            else
            {
                enumerator = System.Array.Empty<JsonValue>().GetEnumerator();
            }

            return enumerator;
        }

        IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator() => this.List.Array.GetEnumerator();
        IEnumerator<KeyValuePair<string, JsonValue>> IEnumerable<KeyValuePair<string, JsonValue>>.GetEnumerator() => this.List.Dictionary.GetEnumerator();
        bool IReadOnlyDictionary<string, JsonValue>.TryGetValue(string key, out JsonValue value) => this.List.Dictionary.TryGetValue(key, out value);
        bool IReadOnlyDictionary<string, JsonValue>.ContainsKey(string key) => this.List.Dictionary.ContainsKey(key);
        IEnumerable<string> IReadOnlyDictionary<string, JsonValue>.Keys => this.List.Dictionary.Keys;
        IEnumerable<JsonValue> IReadOnlyDictionary<string, JsonValue>.Values => this.List.Dictionary.Values;
        int IReadOnlyCollection<KeyValuePair<string, JsonValue>>.Count => this.List.Dictionary.Count;
        int IReadOnlyCollection<JsonValue>.Count => this.List.Array.Count;
        JsonValue IReadOnlyDictionary<string, JsonValue>.this[string key] => this.List.Dictionary[key];
        JsonValue IReadOnlyList<JsonValue>.this[int index] => this.List.Array[index];

        ////////////////////////////////////////
        // JSON Path

        public IEnumerable<JsonValue> Select(string path)
        {
            Query query = this.Context.GetQuery(path);
            return query.Select(this);
        }

        public JsonValue SelectOne(string path) => JsonException.Wrap(() =>
        {
            return this.Select(path).Single();
        });

        ////////////////////////////////////////
        // ValueToString

        public override string ToString() => this.ToString(formatted: false);

        public string ToString(bool formatted)
        {
            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                this.ToString(writer, formatted);
                return writer.ToString();
            }
        }

        public void ToString(TextWriter writer, bool formatted) => JsonException.Wrap(() =>
        {
            TextSerializer.Serialize(new ParsedItemizer(this), Constants.GetIndent(formatted), writer);
        });

        ////////////////////////////////////////
        // ValueToObject

        public T ToObject<T>() => (T)this.ToObject(typeof(T));

        public object ToObject(Type type) => JsonException.Wrap(() =>
        {
            return Deserializer.Deserialize(new ParsedItemizer(this), null, type);
        });

        public void IntoObject(object instance) => JsonException.Wrap(() =>
        {
            Deserializer.Deserialize(new ParsedItemizer(this), instance, null);
        });

        ////////////////////////////////////////
        // StringToString

        public static string StringToString(string text, bool formatted) => JsonException.Wrap(() =>
        {
            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                TextSerializer.Serialize(new TokenItemizer(new StringTokenizer(text)), Constants.GetIndent(formatted), writer);
                return writer.ToString();
            }
        });

        public static void StringToString(TextReader reader, TextWriter writer, bool formatted) => JsonException.Wrap(() =>
        {
            TextSerializer.Serialize(new TokenItemizer(new StreamTokenizer(reader)), Constants.GetIndent(formatted), writer);
        });

        ////////////////////////////////////////
        // StringToValue

        public static JsonValue StringToValue(string text) => JsonException.Wrap(() =>
        {
            return StringParser.Parse(text);
        });

        public static JsonValue StringToValue(TextReader reader) => JsonException.Wrap(() =>
        {
            return StreamParser.Parse(reader);
        });

        ////////////////////////////////////////
        // StringToObject

        public static T StringToObject<T>(string text) => (T)JsonValue.StringToObject(text, typeof(T));

        public static object StringToObject(string text, Type type) => JsonException.Wrap(() =>
        {
            return Deserializer.Deserialize(new TokenItemizer(new StringTokenizer(text)), null, type);
        });

        public static T StringToObject<T>(TextReader reader) => (T)JsonValue.StringToObject(reader, typeof(T));

        public static object StringToObject(TextReader reader, Type type) => JsonException.Wrap(() =>
        {
            return Deserializer.Deserialize(new TokenItemizer(new StreamTokenizer(reader)), null, type);
        });

        public static void StringIntoObject(string text, object instance) => JsonException.Wrap(() =>
        {
            Deserializer.Deserialize(new TokenItemizer(new StringTokenizer(text)), instance, null);
        });

        public static void StringIntoObject(TextReader reader, object instance) => JsonException.Wrap(() =>
        {
            Deserializer.Deserialize(new TokenItemizer(new StreamTokenizer(reader)), instance, null);
        });

        ////////////////////////////////////////
        // ObjectToString

        public static string ObjectToString(object value, bool formatted)
        {
            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                JsonValue.ObjectToString(value, writer, formatted);
                return writer.ToString();
            }
        }

        public static void ObjectToString(object value, TextWriter writer, bool formatted) => JsonException.Wrap(() =>
        {
            TextSerializer.Serialize(new ObjectItemizer(value), Constants.GetIndent(formatted), writer);
        });

        ////////////////////////////////////////
        // ObjectToValue

        public static JsonValue ObjectToValue(object value) => JsonException.Wrap(() =>
        {
            return ObjectParser.Parse(value);
        });

        ////////////////////////////////////////
        // Dynamic

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new ValueDynamic(this, parameter);

        object IValueDynamic.Convert(Type type) => this.ToObject(type);

        object IValueDynamic.GetIndex(object[] indexes)
        {
            JsonValue result = this;

            for (int i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] is int intIndex)
                {
                    result = result[intIndex];
                }
                else if (indexes[i] is string stringIndex)
                {
                    if (int.TryParse(stringIndex, out int intIndex2))
                    {
                        result = result[intIndex2];
                    }
                    else
                    {
                        result = result[stringIndex];
                    }
                }
                else
                {
                    // Force an invalid value
                    result = result[-1];
                }
            }

            if (!result.IsValid)
            {
                throw new IndexOutOfRangeException($"[{string.Join(",", indexes)}]");
            }

            return result;
        }

        object IValueDynamic.GetMember(string name)
        {
            JsonValue value = this[name];
            if (!value.IsValid)
            {
                throw new KeyNotFoundException(name);
            }

            return !value.IsNull ? value : null;
        }

        ////////////////////////////////////////
        // Debug

        private class DebuggerView
        {
            private readonly JsonValue value;

            public DebuggerView(JsonValue value)
            {
                this.value = value;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Value => this.value.Value;
        }
    }
}
