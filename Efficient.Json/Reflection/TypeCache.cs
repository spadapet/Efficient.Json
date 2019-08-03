using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Efficient.Json.Utility;

namespace Efficient.Json.Reflection
{
    /// <summary>
    /// Avoids repeated use of reflection by caching info about a Type that's necessary for serialization
    /// </summary>
    internal class TypeCache
    {
        public static TypeCache Instance { get; } = new TypeCache();

        private readonly ConcurrentDictionary<Type, TypeInfo> typeInfos;
        private readonly ConcurrentDictionary<TypeInfo, TypeInfo> typeToConstruct;

        private TypeCache()
        {
            this.typeInfos = new ConcurrentDictionary<Type, TypeInfo>();
            this.typeToConstruct = new ConcurrentDictionary<TypeInfo, TypeInfo>();
        }

        public TypeInfo GetTypeInfo(Type type)
        {
            return (type != null) ? this.typeInfos.GetOrAdd(type, t => new TypeInfo(this, t)) : null;
        }

        public object CreateInstance(TypeInfo typeInfo, int initialCapacity, out TypeInfo resultTypeInfo)
        {
            resultTypeInfo = this.typeToConstruct.GetOrAdd(typeInfo, this.ComputeTypeToConstruct);

            if (resultTypeInfo != null)
            {
                if (resultTypeInfo.CapacityConstructor is Func<int, object> capacityConstructor)
                {
                    if (typeInfo.IsArray)
                    {
                        initialCapacity = (initialCapacity > 0) ? initialCapacity : Constants.ArrayDeserializeBufferSize;
                    }

                    return capacityConstructor(initialCapacity);
                }
                else if (resultTypeInfo.DefaultConstructor is Func<object> defaultConstructor)
                {
                    return defaultConstructor();
                }
                else
                {
                    return Activator.CreateInstance(resultTypeInfo.Type);
                }
            }

            throw JsonException.New(Resources.Convert_CreateFailed, typeInfo.Type.FullName);
        }

        private TypeInfo ComputeTypeToConstruct(TypeInfo typeInfo)
        {
            if (typeInfo.IsArray)
            {
                if (typeInfo.Type.GetArrayRank() == 1)
                {
                    Type listType = typeof(List<>).MakeGenericType(new Type[] { typeInfo.ElementTypeInfo.Type });
                    typeInfo = this.GetTypeInfo(listType);
                }
                else
                {
                    // No support for multi-dimension arrays (yet?)
                    typeInfo = null;
                }
            }
            else if (typeInfo.IsInterface)
            {
                if (typeInfo.IsConstructedGenericType)
                {
                    Type[] args = typeInfo.Type.GenericTypeArguments;
                    switch (args.Length)
                    {
                        case 1:
                            Type genericList = typeof(List<>).MakeGenericType(args);
                            typeInfo = typeInfo.Type.IsAssignableFrom(genericList) ? this.GetTypeInfo(genericList) : null;
                            break;

                        case 2:
                            Type genericDict = typeof(Dictionary<,>).MakeGenericType(args);
                            typeInfo = typeInfo.Type.IsAssignableFrom(genericDict) ? this.GetTypeInfo(genericDict) : null;
                            break;

                        default:
                            // Not sure what kind of collection this would be
                            typeInfo = null;
                            break;
                    }
                }
                else if (typeInfo.Type == typeof(IDictionary))
                {
                    typeInfo = this.GetTypeInfo(typeof(Dictionary<object, object>));
                }
                else if (typeInfo.Type == typeof(IList) || typeInfo.Type == typeof(IEnumerable))
                {
                    typeInfo = this.GetTypeInfo(typeof(List<object>));
                }
            }
            else if (typeInfo.Type == typeof(object))
            {
                typeInfo = this.GetTypeInfo(typeof(List<object>));
            }

            return typeInfo;
        }
    }
}
