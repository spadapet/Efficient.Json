using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Efficient.Json.Reflection
{
    /// <summary>
    /// Caches reflection info about one Type
    /// </summary>
    [DebuggerDisplay("{Type.FullName} ({flags})")]
    internal class TypeInfo
    {
        public Type Type { get; }
        public TypeCache TypeCache { get; }
        public TypeInfo BaseTypeInfo { get; }
        public Type GenericTypeDefinition { get; }
        private readonly TypeFlags flags;
        private readonly Type elementType;
        private readonly Lazy<TypeReflection> typeReflection;

        public TypeInfo ElementTypeInfo => this.TypeCache.GetTypeInfo(this.elementType);
        public IReadOnlyList<MemberInfo> Members => this.TypeReflection.Members;
        public Func<object> DefaultConstructor => this.TypeReflection.DefaultConstructor?.Value;
        public Func<int, object> CapacityConstructor => this.TypeReflection.CapacityConstructor?.Value;
        public ICollectionInfo CollectionInfo => this.TypeReflection.CollectionInfo;
        public MemberInfo CollectionMember => this.TypeReflection.CollectionInfo?.AsMember;
        public bool SerializeAsObject => this.TypeReflection.CollectionInfo == null || this.TypeReflection.CollectionInfo.SerializeAsObject;
        private TypeReflection TypeReflection => this.typeReflection.Value;

        public bool BaseHasDataContract => this.flags.HasFlag(TypeFlags.BaseHasDataContract);
        public bool HasDataContract => this.flags.HasFlag(TypeFlags.HasDataContract);
        public bool IsAbstract => this.flags.HasFlag(TypeFlags.IsAbstract);
        public bool IsArray => this.flags.HasFlag(TypeFlags.IsArray);
        public bool IsClass => this.flags.HasFlag(TypeFlags.IsClass);
        public bool IsConstructedGenericType => this.flags.HasFlag(TypeFlags.IsConstructedGenericType);
        public bool IsGenericTypeDefinition => this.flags.HasFlag(TypeFlags.IsGenericTypeDefinition);
        public bool IsInterface => this.flags.HasFlag(TypeFlags.IsInterface);
        public bool IsKeyValuePair => this.flags.HasFlag(TypeFlags.IsKeyValuePair);
        public bool IsNullable => this.flags.HasFlag(TypeFlags.IsNullable);
        public bool IsPrimitive => this.flags.HasFlag(TypeFlags.IsPrimitive);
        public bool IsSimple => this.flags.HasFlag(TypeFlags.IsSimple);
        public bool IsValueType => this.flags.HasFlag(TypeFlags.IsValueType);

        [Flags]
        private enum TypeFlags
        {
            None = 0x0000,
            BaseHasDataContract = 0x0001,
            HasDataContract = 0x0002,
            IsAbstract = 0x0004,
            IsArray = 0x0008,
            IsClass = 0x0010,
            IsConstructedGenericType = 0x0020,
            IsGenericTypeDefinition = 0x0040,
            IsInterface = 0x0080,
            IsKeyValuePair = 0x0100,
            IsNullable = 0x0200,
            IsPrimitive = 0x0400,
            IsSimple = 0x0800,
            IsValueType = 0x1000,
        }

        public TypeInfo(TypeCache typeCache, Type type)
        {
            Type nullable = Nullable.GetUnderlyingType(type);

            this.Type = type;
            this.TypeCache = typeCache;
            this.BaseTypeInfo = typeCache.GetTypeInfo(type.BaseType);
            this.GenericTypeDefinition = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : null;
            this.elementType = nullable ?? (type.IsArray ? type.GetElementType() : null);
            this.typeReflection = new Lazy<TypeReflection>(() => new TypeReflection(this));
            bool hasDataContract = this.Type.GetCustomAttributesData().Any(d => typeof(DataContractAttribute).IsAssignableFrom(d.AttributeType));

            this.flags =
                (hasDataContract ? TypeFlags.HasDataContract : TypeFlags.None) |
                (type.IsAbstract ? TypeFlags.IsAbstract : TypeFlags.None) |
                (this.elementType != null ? TypeFlags.IsArray : TypeFlags.None) |
                (type.IsClass ? TypeFlags.IsClass : TypeFlags.None) |
                (this.GenericTypeDefinition != null ? TypeFlags.IsConstructedGenericType : TypeFlags.None) |
                (type.IsGenericTypeDefinition ? TypeFlags.IsGenericTypeDefinition : TypeFlags.None) |
                (type.IsInterface ? TypeFlags.IsInterface : TypeFlags.None) |
                (this.GenericTypeDefinition == typeof(KeyValuePair<,>) ? TypeFlags.IsKeyValuePair : TypeFlags.None) |
                (nullable != null ? TypeFlags.IsNullable : TypeFlags.None) |
                (type.IsPrimitive ? TypeFlags.IsPrimitive : TypeFlags.None) |
                (type.IsValueType ? TypeFlags.IsValueType : TypeFlags.None);

            if (this.IsPrimitive || (this.IsValueType && (type == typeof(decimal) || type == typeof(DateTime))) || type == typeof(string))
            {
                this.flags |= TypeFlags.IsSimple;
            }

            for (TypeInfo baseTypeInfo = this.BaseTypeInfo; baseTypeInfo != null; baseTypeInfo = baseTypeInfo.BaseTypeInfo)
            {
                if (baseTypeInfo.HasDataContract)
                {
                    this.flags |= TypeFlags.BaseHasDataContract;
                    break;
                }
            }
        }

        public MemberInfo TryFindMember(string name)
        {
            TypeReflection data = this.TypeReflection;
            return (name != null && data.NameToMember != null && data.NameToMember.TryGetValue(name, out MemberInfo info)) ? info : null;
        }

        public IEnumerator<KeyValuePair<object, object>> GetValues(object instance)
        {
            if (this.IsArray)
            {
                return this.ElementTypeInfo.IsKeyValuePair
                    ? Reflection.DictionaryInfo.GetValuesFromDictionary(instance, this.TypeCache)
                    : Reflection.CollectionInfo.GetValuesFromEnumerable(instance);
            }

            if (this.TypeReflection.CollectionInfo is ICollectionInfo collectionInfo)
            {
                return collectionInfo.GetValues(instance);
            }

            return this.GetObjectValues(instance);
        }

        private IEnumerator<KeyValuePair<object, object>> GetObjectValues(object instance)
        {
            foreach (MemberInfo info in this.Members.Where(m => !m.IsIgnored))
            {
                object value = info.GetObject(instance, null);
                if (info.EmitDefaultValue || !info.ValueType.IsValueTypeDefault(value))
                {
                    yield return new KeyValuePair<object, object>(info.Name, value);
                }
            }
        }

        private bool IsValueTypeDefault(object value)
        {
            if (this.TypeReflection.IsValueTypeDefault?.Value is Func<object, bool> func)
            {
                return func(value);
            }

            Debug.Assert(!this.IsValueType);
            return value == null;
        }

        public Array ToArray(object instance)
        {
            if (instance == null || this.IsArray)
            {
                return (Array)instance;
            }

            return this.TypeReflection.ToArrayMethod?.Value.Invoke(instance);
        }

        public KeyValuePair<object, object> ToKeyValuePair(object instance)
        {
            if (instance == null)
            {
                return default;
            }

            return this.TypeReflection.ToKeyValuePairMethod?.Value.Invoke(instance) ?? default;
        }

        public void OnDeserializing(object instance)
        {
            if (this.BaseHasDataContract)
            {
                this.BaseTypeInfo.OnDeserializing(instance);
            }

            if (this.HasDataContract)
            {
                this.TypeReflection.DeserializingMethod?.Value.Invoke(instance, default);
            }
        }

        public void OnDeserialized(object instance)
        {
            if (this.BaseHasDataContract)
            {
                this.BaseTypeInfo.OnDeserialized(instance);
            }

            if (this.HasDataContract)
            {
                this.TypeReflection.DeserializedMethod?.Value.Invoke(instance, default);
            }
        }

        public void OnSerializing(object instance)
        {
            if (this.BaseHasDataContract)
            {
                this.BaseTypeInfo.OnSerializing(instance);
            }

            if (this.HasDataContract)
            {
                this.TypeReflection.SerializingMethod?.Value.Invoke(instance, default);
            }
        }

        public void OnSerialized(object instance)
        {
            if (this.BaseHasDataContract)
            {
                this.BaseTypeInfo.OnSerialized(instance);
            }

            if (this.HasDataContract)
            {
                this.TypeReflection.SerializedMethod?.Value.Invoke(instance, default);
            }
        }
    }
}
