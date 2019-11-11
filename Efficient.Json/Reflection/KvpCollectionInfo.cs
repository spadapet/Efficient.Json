using Efficient.Json.Utility;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Efficient.Json.Reflection
{
    internal class KvpCollectionInfo : MemberInfo, ICollectionInfo
    {
        public static ICollectionInfo TryCreate(TypeInfo typeInfo, System.Reflection.PropertyInfo info, System.Reflection.MethodInfo addMethod, System.Reflection.ParameterInfo[] addMethodParameters)
        {
            if (addMethodParameters.Length == 1)
            {
                TypeInfo valueType = typeInfo.TypeCache.GetTypeInfo(addMethodParameters[0].ParameterType);
                TypeInfo keyType = null;
                TypeInfo kvpType = null;
                bool mustBeKvp = valueType.IsKeyValuePair;

                if (mustBeKvp)
                {
                    Type[] args = valueType.Type.GetGenericArguments();
                    kvpType = valueType;
                    keyType = typeInfo.TypeCache.GetTypeInfo(args[0]);
                    valueType = typeInfo.TypeCache.GetTypeInfo(args[1]);
                }
                else if (valueType.Type == typeof(object))
                {
                    kvpType = typeInfo.TypeCache.GetTypeInfo(typeof(KeyValuePair<string, object>));
                    keyType = typeInfo.TypeCache.GetTypeInfo(typeof(string));
                }

                if (kvpType != null)
                {
                    return new KvpCollectionInfo(typeInfo, info, addMethod, mustBeKvp, kvpType, keyType, valueType);
                }
            }

            return null;
        }

        private readonly Lazy<Action<object, object>> callAdd;
        private readonly Lazy<Func<object, object, object>> constructKvp;
        private readonly bool mustBeKvp;

        private KvpCollectionInfo(
            TypeInfo typeInfo,
            System.Reflection.PropertyInfo info,
            System.Reflection.MethodInfo addMethod,
            bool mustBeKvp,
            TypeInfo kvpType,
            TypeInfo keyType,
            TypeInfo valueType)
            : base(typeInfo, info, keyType, valueType)
        {
            this.mustBeKvp = mustBeKvp;

            this.constructKvp = new Lazy<Func<object, object, object>>(() =>
            {
                System.Reflection.ConstructorInfo kvpConstructor = kvpType.Type.GetConstructor(new Type[] { keyType.Type, valueType.Type });

                ParameterExpression keyParam = Expression.Parameter(typeof(object), null);
                ParameterExpression valueParam = Expression.Parameter(typeof(object), null);
                Expression castKey = ReflectionUtility.Convert(keyParam, keyType);
                Expression castValue = ReflectionUtility.Convert(valueParam, valueType);
                Expression newKvp = Expression.New(kvpConstructor, castKey, castValue);
                Expression convertKvp = Expression.Convert(newKvp, typeof(object));

                return ReflectionUtility.CompileLambda<Func<object, object, object>>(convertKvp, keyParam, valueParam);
            });

            this.callAdd = new Lazy<Action<object, object>>(() =>
            {
                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                ParameterExpression valueParam = Expression.Parameter(typeof(object), null);
                Expression castTarget = ReflectionUtility.Convert(targetParam, typeInfo);
                Expression castValue = ReflectionUtility.Convert(valueParam, mustBeKvp ? kvpType : valueType);
                Expression callAdd = Expression.Call(castTarget, addMethod, castValue);

                return ReflectionUtility.CompileLambda<Action<object, object>>(callAdd, targetParam, valueParam);
            });
        }

        protected override object GetMemberObject(object target, object key)
        {
            return null;
        }

        protected override void SetMemberObject(object target, object key, object value)
        {
            if (this.mustBeKvp || key != null)
            {
                value = this.constructKvp.Value(key ?? this.GenerateKey(target), value);
            }

            this.callAdd.Value(target, value);
        }

        bool ICollectionInfo.SerializeAsObject => this.mustBeKvp;
        MemberInfo ICollectionInfo.AsMember => this;

        IEnumerator<KeyValuePair<object, object>> ICollectionInfo.GetValues(object instance)
        {
            return this.mustBeKvp
                ? DictionaryInfo.GetValuesFromDictionary(instance, this.TypeInfo.TypeCache)
                : CollectionInfo.GetValuesFromEnumerable(instance);
        }
    }
}
