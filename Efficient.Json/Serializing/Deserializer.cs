using Efficient.Json.Itemizing;
using Efficient.Json.Reflection;
using Efficient.Json.Tokens;
using Efficient.Json.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Efficient.Json.Serializing
{
    internal class Deserializer : RecursiveBase
    {
        private readonly Itemizer itemizer;
        private readonly TypeCache types;
        private readonly Dictionary<TypeInfo, Stack<KeyValuePair<TypeInfo, IList>>> arrayBank;
        private readonly BufferBank<MemberValue> valueBank;

        public static object Deserialize(Itemizer itemizer, object instance, Type type)
        {
            Deserializer deserializer = new Deserializer(itemizer);
            Result result = new Result(instance);
            FieldInfo memberInfo = new FieldInfo(deserializer.types.GetTypeInfo(typeof(Result)), Result.ValueField, type ?? instance?.GetType());

            MemberValue value = deserializer.DeserializeValue(itemizer.NextItem(), result, nameof(Result.value), memberInfo);
            Debug.Assert(itemizer.NextItem().Type == ItemType.End);

            value.SetValue(deserializer, result);
            return result.value;
        }

        private Deserializer(Itemizer itemizer)
        {
            this.itemizer = itemizer;
            this.types = TypeCache.Instance;
            this.arrayBank = new Dictionary<TypeInfo, Stack<KeyValuePair<TypeInfo, IList>>>();
            this.valueBank = new BufferBank<MemberValue>();
        }

        private object BorrowArrayBuffer(TypeInfo typeInfo, int initialCapacity, object oldInstance, out TypeInfo bufferTypeInfo)
        {
            IList instance;

            if (this.arrayBank.TryGetValue(typeInfo, out Stack<KeyValuePair<TypeInfo, IList>> buffers) && buffers.Count > 0)
            {
                KeyValuePair<TypeInfo, IList> kvp = buffers.Pop();
                bufferTypeInfo = kvp.Key;
                instance = kvp.Value;
            }
            else
            {
                instance = (IList)this.types.CreateInstance(typeInfo, initialCapacity, out bufferTypeInfo);
            }

            if (oldInstance is IList oldList)
            {
                foreach (object obj in oldList)
                {
                    instance.Add(obj);
                }
            }

            return instance;
        }

        private void ReturnArrayBuffer(TypeInfo typeInfo, object instance, TypeInfo bufferTypeInfo)
        {
            IList list = (IList)instance;
            if (!this.arrayBank.TryGetValue(typeInfo, out Stack<KeyValuePair<TypeInfo, IList>> buffers))
            {
                buffers = new Stack<KeyValuePair<TypeInfo, IList>>();
                this.arrayBank.Add(typeInfo, buffers);
            }

            list.Clear();
            buffers.Push(new KeyValuePair<TypeInfo, IList>(bufferTypeInfo, list));
        }

        private void DeserializeObjectInto(object instance, TypeInfo typeInfo)
        {
            List<MemberValue> values = (typeInfo != null && (typeInfo.HasDataContract || typeInfo.BaseHasDataContract)) ? this.valueBank.Borrow() : null;

            for (Item keyItem = this.itemizer.NextItem(); keyItem.Type != ItemType.ObjectEnd; keyItem = this.itemizer.NextItem())
            {
                Key key = this.itemizer.ValueToken.DecodedKey;
                MemberInfo memberInfo = (typeInfo != null) ? (typeInfo.CollectionMember ?? typeInfo.TryFindMember(key.Text)) : null;
                MemberValue value = this.DeserializeValue(this.itemizer.NextItem(), instance, key.Text, memberInfo);

                if (value.IsValid)
                {
                    if (values != null)
                    {
                        values.Add(value);
                    }
                    else
                    {
                        value.SetValue(this, instance);
                    }
                }
            }

            if (values != null)
            {
                values.Sort(MemberValue.ComparisonFunc);

                foreach (MemberValue value in values)
                {
                    value.SetValue(this, instance);
                }

                this.valueBank.Return(values);
            }
        }

        private void DeserializeArrayInto(object instance, TypeInfo typeInfo)
        {
            MemberInfo memberInfo = typeInfo?.CollectionMember;

            for (Item item = this.itemizer.NextItem(); item.Type != ItemType.ArrayEnd; item = this.itemizer.NextItem())
            {
                MemberValue value = this.DeserializeValue(item, instance, null, memberInfo);
                if (value.IsValid)
                {
                    value.SetValue(this, instance);
                }
            }
        }

        private MemberValue DeserializeObjectOrArray(Item openItem, object parent, string key, MemberInfo memberInfo)
        {
            TypeInfo typeInfo = null;
            object instance = null;
            bool isNew = false;

            if (memberInfo != null)
            {
                instance = memberInfo.GetObject(parent, key);
                isNew = (instance == null || memberInfo.ValueType.IsArray);

                if (isNew)
                {
                    instance = memberInfo.ValueType.IsArray
                        ? this.BorrowArrayBuffer(memberInfo.ValueType, openItem.Size, instance, out typeInfo)
                        : this.types.CreateInstance(memberInfo.ValueType, openItem.Size, out typeInfo);
                    typeInfo.OnDeserializing(instance);
                }
                else
                {
                    typeInfo = this.types.GetTypeInfo(instance.GetType());
                }
            }

            if (openItem.Type == ItemType.ArrayStart)
            {
                this.DeserializeArrayInto(instance, typeInfo);
            }
            else
            {
                this.DeserializeObjectInto(instance, typeInfo);
            }

            if (isNew)
            {
                typeInfo.OnDeserialized(instance);
            }

            if (isNew || (typeInfo != null && typeInfo.IsValueType))
            {
                return new MemberValue(memberInfo, key, instance, memberInfo.ValueType.IsArray ? typeInfo : null);
            }

            return MemberValue.Invalid;
        }

        private MemberValue DeserializeValue(Item item, object parent, string key, MemberInfo memberInfo)
        {
            switch (item.Type)
            {
                default:
                    Debug.Fail($"Unexpected item type: {item.Type}");
                    break;

                case ItemType.ArrayStart:
                case ItemType.ObjectStart:
                    return this.DeserializeObjectOrArray(item, parent, key, memberInfo);

                case ItemType.Value:
                    if (memberInfo != null)
                    {
                        FullToken token = this.itemizer.ValueToken;
                        object value = null;

                        switch (token.Type)
                        {
                            case TokenType.True:
                                value = Constants.TrueObject;
                                break;

                            case TokenType.False:
                                value = Constants.FalseObject;
                                break;

                            case TokenType.Number:
                                value = (item.Data is decimal)
                                    ? item.Data
                                    : decimal.Parse((item.Data is string stringForDecimal) ? stringForDecimal : token.DecodedKey.Text);
                                break;

                            case TokenType.String:
                                value = (item.Data is string stringForString) ? stringForString : token.DecodedKey.Text;
                                break;

                            case TokenType.EncodedString:
                                value = token.DecodedKey.Text;
                                break;
                        }

                        return new MemberValue(memberInfo, key, value);
                    }
                    break;
            }

            return MemberValue.Invalid;
        }

        private class Result
        {
            public static System.Reflection.FieldInfo ValueField = typeof(Result).GetField(nameof(Result.value));
            public object value;

            public Result(object value)
            {
                this.value = value;
            }
        }

        private struct MemberValue
        {
            public MemberInfo Member { get; }
            public string Key { get; }
            public object Value { get; }
            private readonly TypeInfo borrowedArrayTypeInfo;

            public bool IsValid => this.Member != null;
            public static MemberValue Invalid => default;

            public MemberValue(MemberInfo memberInfo, string key, object value, TypeInfo borrowedArrayTypeInfo = null)
            {
                this.Member = memberInfo;
                this.Key = key;
                this.Value = value;
                this.borrowedArrayTypeInfo = borrowedArrayTypeInfo;
            }

            public void SetValue(Deserializer deserializer, object parent)
            {
                this.Member.SetObject(parent, this.Key, this.Value);

                if (this.borrowedArrayTypeInfo != null)
                {
                    deserializer.ReturnArrayBuffer(this.Member.ValueType, this.Value, this.borrowedArrayTypeInfo);
                }
            }

            public static Comparison<MemberValue> ComparisonFunc { get; } = MemberValue.Compare;

            public static int Compare(MemberValue x, MemberValue y)
            {
                return x.Member.CompareTo(y.Member);
            }
        }
    }
}
