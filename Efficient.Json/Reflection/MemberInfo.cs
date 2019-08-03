using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;

namespace Efficient.Json.Reflection
{
    /// <summary>
    /// Caches reflection info about one member (either field or property)
    /// </summary>
    [DebuggerDisplay("{Name}: {ValueType.Type.Name} ({flags})")]
    internal abstract class MemberInfo : IComparable<MemberInfo>, IComparable
    {
        public string Name { get; private set; }
        public int Order { get; private set; }
        public TypeInfo TypeInfo { get; }
        public TypeInfo KeyType { get; }
        public TypeInfo ValueType { get; }
        private Flags flags;

        public bool IsOrderSet => this.flags.HasFlag(Flags.IsOrderSet);
        public bool IsRequired => this.flags.HasFlag(Flags.IsRequired);
        public bool IsIgnored => this.flags.HasFlag(Flags.IsIgnored);
        public bool EmitDefaultValue => !this.flags.HasFlag(Flags.IgnoreDefaultValue);
        private bool KeyNullable => this.flags.HasFlag(Flags.KeyNullable);
        private bool ValueNullable => this.flags.HasFlag(Flags.ValueNullable);

        [Flags]
        private enum Flags
        {
            None = 0,
            KeyNullable = 0x01,
            ValueNullable = 0x02,
            IsOrderSet = 0x04,
            IsRequired = 0x08,
            IsIgnored = 0x10,
            IgnoreDefaultValue = 0x20,
        }

        protected MemberInfo(TypeInfo typeInfo, System.Reflection.MemberInfo info, TypeInfo keyType, TypeInfo valueType)
        {
            this.flags |= (keyType != null && keyType.IsNullable) ? Flags.KeyNullable : Flags.None;
            this.flags |= (valueType != null && valueType.IsNullable) ? Flags.ValueNullable : Flags.None;

            this.TypeInfo = typeInfo;
            this.Name = info.Name;
            this.KeyType = this.KeyNullable ? keyType.ElementTypeInfo : keyType;
            this.ValueType = this.ValueNullable ? valueType.ElementTypeInfo : valueType;

            if (typeInfo.HasDataContract)
            {
                this.UpdateFromDataContract(info);
            }
        }

        protected abstract object GetMemberObject(object target, object key);
        protected abstract void SetMemberObject(object target, object key, object value);

        public object GetObject(object target, string key)
        {
            object getKey = MemberInfo.ConvertObject(key, this.KeyType, this.KeyNullable);
            return this.GetMemberObject(target, getKey);
        }

        public void SetObject(object target, string key, object value)
        {
            object setKey = (key != null) ? MemberInfo.ConvertObject(key, this.KeyType, this.KeyNullable) : null;
            object setValue = MemberInfo.ConvertObject(value, this.ValueType, this.ValueNullable);
            this.SetMemberObject(target, setKey, setValue);
        }

        protected object GenerateKey(object target)
        {
            if (this.KeyType != null && target is ICollection collection)
            {
                return MemberInfo.ConvertObject(collection.Count, this.KeyType, this.KeyNullable);
            }

            return null;
        }

        private void UpdateFromDataContract(System.Reflection.MemberInfo info)
        {
            bool isDataMember = false;

            foreach (System.Reflection.CustomAttributeData data in info.CustomAttributes)
            {
                if (typeof(IgnoreDataMemberAttribute).IsAssignableFrom(data.AttributeType))
                {
                    this.flags |= Flags.IsIgnored;
                }
                else if (typeof(DataMemberAttribute).IsAssignableFrom(data.AttributeType))
                {
                    isDataMember = true;

                    foreach (System.Reflection.CustomAttributeNamedArgument arg in data.NamedArguments)
                    {
                        switch (arg.MemberName)
                        {
                            case "Name":
                                this.Name = (string)arg.TypedValue.Value;
                                break;

                            case "EmitDefaultValue":
                                if ((bool)arg.TypedValue.Value)
                                {
                                    this.flags &= ~Flags.IgnoreDefaultValue;
                                }
                                else
                                {
                                    this.flags |= Flags.IgnoreDefaultValue;
                                }
                                break;

                            case "IsRequired":
                                if ((bool)arg.TypedValue.Value)
                                {
                                    this.flags |= Flags.IsRequired;
                                }
                                else
                                {
                                    this.flags &= ~Flags.IsRequired;
                                }
                                break;

                            case "Order":
                                this.flags |= Flags.IsOrderSet;
                                this.Order = (int)arg.TypedValue.Value;
                                break;
                        }
                    }
                }
            }

            if (!isDataMember)
            {
                this.Name = string.Empty;
            }
        }

        protected static object ConvertObject(object value, TypeInfo typeInfo, bool nullable)
        {
            if (typeInfo == null)
            {
                value = null;
            }
            else if (value != null)
            {
                if (typeInfo.Type != typeof(object) && !typeInfo.Type.IsAssignableFrom(value.GetType()))
                {
                    if (typeInfo.IsArray)
                    {
                        TypeInfo valueTypeInfo = typeInfo.TypeCache.GetTypeInfo(value.GetType());
                        value = valueTypeInfo.ToArray(value) ?? MemberInfo.CreateArray(value, typeInfo);
                    }
                    else if (value is IConvertible valueConvert)
                    {
                        value = valueConvert.ToType(typeInfo.Type, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        throw JsonException.New(Resources.Convert_TypeFailed, value, typeInfo.Type.FullName);
                    }
                }
            }
            else if (typeInfo.IsValueType && !nullable)
            {
                throw JsonException.New(Resources.Convert_TypeFailed, "null", typeInfo.Type.FullName);
            }

            return value;
        }

        private static Array CreateArray(object value, TypeInfo arrayTypeInfo)
        {
            IList valueList = value as IList;
            Array array = (Array)arrayTypeInfo.CapacityConstructor(valueList != null ? valueList.Count : 1);
            TypeInfo elementType = arrayTypeInfo.ElementTypeInfo;

            bool nullableElementType = elementType.IsNullable;
            if (nullableElementType)
            {
                elementType = elementType.ElementTypeInfo;
            }

            if (valueList != null)
            {
                int i = 0;
                foreach (object child in valueList)
                {
                    array.SetValue(MemberInfo.ConvertObject(child, elementType, nullableElementType), i++);
                }
            }
            else
            {
                array.SetValue(MemberInfo.ConvertObject(value, elementType, nullableElementType), 0);
            }

            return array;
        }

        public int CompareTo(MemberInfo other)
        {
            if (this.TypeInfo != other.TypeInfo)
            {
                return other.TypeInfo.Type.IsAssignableFrom(this.TypeInfo.Type) ? 1 : -1;
            }
            else if (this.IsOrderSet == other.IsOrderSet)
            {
                if (!this.IsOrderSet || this.Order == other.Order)
                {
                    return string.CompareOrdinal(this.Name, other.Name);
                }

                return this.Order > other.Order ? 1 : -1;
            }
            else
            {
                return this.IsOrderSet ? 1 : -1;
            }
        }

        int IComparable.CompareTo(object obj)
        {
            return this.CompareTo((MemberInfo)obj);
        }
    }
}
