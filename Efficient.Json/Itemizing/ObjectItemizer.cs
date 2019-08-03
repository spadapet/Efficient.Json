using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Efficient.Json.Reflection;
using Efficient.Json.Tokens;
using Efficient.Json.Utility;

namespace Efficient.Json.Itemizing
{
    internal class ObjectItemizer : Itemizer
    {
        private FullToken token;
        private TypeCache typeCache;
        private Stack<State> states;
        private State state;

        private enum StateType { RootValue, ArrayValue, ObjectKey, ObjectValue }

        private struct State
        {
            public object value;
            public TypeInfo typeInfo;
            public StateType type;
            public IEnumerator<KeyValuePair<object, object>> memberEnumerator;
        }

        public ObjectItemizer(object value)
        {
            this.token = FullToken.Empty;
            this.typeCache = TypeCache.Instance;
            this.states = new Stack<State>(Constants.BufferSize);

            this.state = new State()
            {
                value = value,
                typeInfo = this.typeCache.GetTypeInfo(value?.GetType()),
            };
        }

        public override FullToken ValueToken => this.token;

        public override Item NextItem()
        {
            Item item = default;
            object value = null;
            bool valueSet = false;

            switch (this.state.type)
            {
                case StateType.RootValue:
                    if (this.state.memberEnumerator == null)
                    {
                        this.state.memberEnumerator = Enumerable.Empty<KeyValuePair<object, object>>().GetEnumerator();
                        value = this.state.value;
                        valueSet = true;
                    }
                    else
                    {
                        item = new Item(ItemType.End);
                    }
                    break;

                case StateType.ArrayValue:
                    if (this.state.memberEnumerator.MoveNext())
                    {
                        Debug.Assert(this.state.memberEnumerator.Current.Key == null);
                        value = this.state.memberEnumerator.Current.Value;
                        valueSet = true;
                    }
                    else
                    {
                        item = new Item(ItemType.ArrayEnd);
                        this.state.memberEnumerator.Dispose();
                        this.state = this.states.Pop();
                    }
                    break;

                case StateType.ObjectValue:
                    value = this.state.memberEnumerator.Current.Value;
                    valueSet = true;
                    this.state.type = StateType.ObjectKey;
                    break;

                case StateType.ObjectKey:
                    if (this.state.memberEnumerator.MoveNext())
                    {
                        item = new Item(ItemType.Key);
                        this.CreateTokenAndItem(this.state.memberEnumerator.Current.Key, out this.token, out _);
                        this.state.type = StateType.ObjectValue;
                    }
                    else
                    {
                        this.state.typeInfo.OnSerialized(this.state.value);
                        item = new Item(ItemType.ObjectEnd);
                        this.state.memberEnumerator.Dispose();
                        this.state = this.states.Pop();
                    }
                    break;
            }

            if (valueSet)
            {
                this.CreateTokenAndItem(value, out this.token, out item);

                if (item.Type == ItemType.ArrayStart || item.Type == ItemType.ObjectStart)
                {
                    this.states.Push(this.state);
                    this.state.value = value;
                    this.state.type = (item.Type == ItemType.ArrayStart) ? StateType.ArrayValue : StateType.ObjectKey;
                    this.state.typeInfo = this.typeCache.GetTypeInfo(value.GetType());
                    this.state.typeInfo.OnSerializing(value);
                    this.state.memberEnumerator = this.state.typeInfo.GetValues(value);
                }
            }

            return item;
        }

        private void CreateTokenAndItem(object obj, out FullToken fullToken, out Item item)
        {
            Token token;
            string tokenText;

            switch (Convert.GetTypeCode(obj))
            {
                case TypeCode.Boolean:
                    {
                        bool asBool = (bool)obj;
                        tokenText = asBool ? "true" : "false";
                        token = asBool
                            ? new Token(TokenType.True, 0, tokenText.Length, StringHasher.TrueHash)
                            : new Token(TokenType.False, 0, tokenText.Length, StringHasher.FalseHash);
                        item = new Item(ItemType.Value);
                    }
                    break;

                case TypeCode.Char:
                case TypeCode.String:
                case TypeCode.DateTime:
                    tokenText = Convert.ToString(obj, CultureInfo.InvariantCulture);
                    token = new Token(TokenType.String, 0, tokenText.Length, StringHasher.Hash(tokenText));
                    item = new Item(ItemType.Value, tokenText);
                    break;

                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    tokenText = Convert.ToString(obj, CultureInfo.InvariantCulture);
                    token = new Token(TokenType.Number, 0, tokenText.Length, StringHasher.Hash(tokenText));
                    item = new Item(ItemType.Value, Convert.ToDecimal(obj, CultureInfo.InvariantCulture));
                    break;

                case TypeCode.DBNull:
                case TypeCode.Empty:
                    tokenText = "null";
                    token = new Token(TokenType.Null, 0, tokenText.Length, StringHasher.NullHash);
                    item = new Item(ItemType.Value);
                    break;

                default:
                case TypeCode.Object:
                    {
                        TypeInfo typeInfo = this.typeCache.GetTypeInfo(obj.GetType());
                        TokenType tokenType = typeInfo.IsArray
                            ? (typeInfo.ElementTypeInfo.IsKeyValuePair ? TokenType.ObjectValue : TokenType.ArrayValue)
                            : (typeInfo.SerializeAsObject ? TokenType.ObjectValue : TokenType.ArrayValue);

                        tokenText = string.Empty;
                        token = new Token(tokenType, 0, 0, StringHasher.NullHash);
                        item = (tokenType == TokenType.ArrayValue)
                            ? new Item(ItemType.ArrayStart, (obj is ICollection collection) ? collection.Count : 0)
                            : new Item(ItemType.ObjectStart);
                    }
                    break;
            }

            fullToken = new FullToken(token, tokenText, 0, 0);
        }
    }
}
