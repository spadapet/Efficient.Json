using Efficient.Json.Itemizing;
using Efficient.Json.Tokens;
using Efficient.Json.Utility;
using Efficient.Json.Value;
using System.Collections.Generic;
using System.Diagnostics;

namespace Efficient.Json.Parsing
{
    /// <summary>
    /// Base parser class that can parse any source of JSON, as long as a derived class
    /// implements the abstract methods.
    /// </summary>
    internal abstract class Parser : RecursiveBase
    {
        private readonly JsonContext context;
        private readonly Itemizer itemizer;
        private readonly Dictionary<Key, Key> keyCache;
        private readonly BufferBank<Key> parseKeys;
        private readonly BufferBank<JsonValue> parseValues;
        private readonly Dictionary<ValueData, JsonValue> values;
        private readonly ValueList emptyValuesArray;
        private readonly ValueList emptyValuesDictionary;

        private readonly static Token ArrayStartToken = new Token(TokenType.ArrayValue, 0, 0, 0);
        private readonly static Token DictionaryStartToken = new Token(TokenType.ObjectValue, 0, 0, 0);
        private readonly static List<Key> EmptyKeys = new List<Key>();
        private readonly static List<JsonValue> EmptyValues = new List<JsonValue>();

        protected Parser(JsonContext context, Itemizer itemizer)
        {
            this.context = context;
            this.itemizer = itemizer;
            this.keyCache = new Dictionary<Key, Key>(Constants.BufferSize, KeyComparer.Instance);
            this.parseKeys = new BufferBank<Key>();
            this.parseValues = new BufferBank<JsonValue>();
            this.values = new Dictionary<ValueData, JsonValue>(Constants.BufferSize, ValueDataComparer.Instance);
            this.emptyValuesArray = new ValueList(Parser.EmptyValues, this.context);
            this.emptyValuesDictionary = new ValueList(Parser.EmptyKeys, Parser.EmptyValues, this.context);
            this.context.InvalidValue = new JsonValue(new ValueData(default, this.emptyValuesArray));
        }

        protected virtual string FinalText => this.context.Text;
        protected virtual Token CacheText(FullToken fullToken) => fullToken.Token;

        protected JsonValue ParseRoot()
        {
            JsonValue value = this.ParseValue(this.itemizer.NextItem(), out Item endItem);
            Debug.Assert(endItem.Type == ItemType.End);
            this.context.Text = this.FinalText;
            return value;
        }

        private JsonValue ParseObject(Item openItem)
        {
            List<Key> parseKeys = this.parseKeys.Borrow(openItem.Size);
            List<JsonValue> parseValues = this.parseValues.Borrow(openItem.Size);

            for (Item keyItem = this.itemizer.NextItem(); keyItem.Type != ItemType.ObjectEnd;)
            {
                Key key = this.itemizer.ValueToken.DecodedKey;
                if (!this.keyCache.TryGetValue(key, out Key cachedKey))
                {
                    cachedKey = key;
                    this.keyCache.Add(key, key);
                }

                parseKeys.Add(cachedKey);
                parseValues.Add(this.ParseValue(this.itemizer.NextItem(), out keyItem));
            }

            ValueList values = (parseValues.Count > 0) ? new ValueList(parseKeys, parseValues, this.context) : this.emptyValuesDictionary;
            this.parseKeys.Return(parseKeys);
            this.parseValues.Return(parseValues);

            return this.CacheValue(openItem, Parser.DictionaryStartToken, values);
        }

        private JsonValue ParseArray(Item openItem)
        {
            List<JsonValue> parseValues = this.parseValues.Borrow(openItem.Size);

            for (Item item = this.itemizer.NextItem(); item.Type != ItemType.ArrayEnd;)
            {
                parseValues.Add(this.ParseValue(item, out item));
            }

            ValueList values = (parseValues.Count > 0) ? new ValueList(parseValues, this.context) : this.emptyValuesArray;
            this.parseValues.Return(parseValues);

            return this.CacheValue(openItem, Parser.ArrayStartToken, values);
        }

        private JsonValue ParseValue(Item item, out Item nextItem)
        {
            JsonValue value;

            switch (item.Type)
            {
                case ItemType.ArrayStart:
                    value = this.ParseArray(item);
                    break;

                case ItemType.ObjectStart:
                    value = this.ParseObject(item);
                    break;

                default:
                    value = this.CacheValue(item, this.CacheText(this.itemizer.ValueToken), this.emptyValuesArray);
                    break;
            }

            nextItem = this.itemizer.NextItem();
            return value;
        }

        private JsonValue CacheValue(Item item, Token token, ValueList values)
        {
            ValueData data = new ValueData(token, values);
            JsonValue value;

            // Don't reuse values with children
            if (!values.IsEmpty)
            {
                value = new JsonValue(data);
            }
            else if (!this.values.TryGetValue(data, out value))
            {
                value = new JsonValue(data, item.Data);
                this.values.Add(data, value);
            }

            return value;
        }
    }
}
