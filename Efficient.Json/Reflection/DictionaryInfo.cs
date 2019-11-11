using Efficient.Json.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Efficient.Json.Reflection
{
    internal class DictionaryInfo : MemberInfo, ICollectionInfo
    {
        public static ICollectionInfo TryCreate(TypeInfo typeInfo, System.Reflection.PropertyInfo info, System.Reflection.ParameterInfo[] addMethodParameters)
        {
            if (addMethodParameters.Length == 2)
            {
                TypeInfo keyType = typeInfo.TypeCache.GetTypeInfo(addMethodParameters[0].ParameterType);
                TypeInfo valueType = typeInfo.TypeCache.GetTypeInfo(addMethodParameters[1].ParameterType);

                return new DictionaryInfo(typeInfo, info, keyType, valueType);
            }

            return null;
        }

        bool ICollectionInfo.SerializeAsObject => true;
        MemberInfo ICollectionInfo.AsMember => this;
        private readonly System.Reflection.PropertyInfo info;
        private readonly Lazy<TryGetValueDelegate> callTryGetValue;
        private readonly Lazy<Func<object, object, object>> callGet;
        private readonly Lazy<Action<object, object, object>> callSet;
        private delegate bool TryGetValueDelegate(object target, object key, out object value);

        private DictionaryInfo(TypeInfo typeInfo, System.Reflection.PropertyInfo info, TypeInfo keyType, TypeInfo valueType)
            : base(typeInfo, info, keyType, valueType)
        {
            this.info = info;

            this.callTryGetValue = new Lazy<TryGetValueDelegate>(() =>
            {
                System.Reflection.MethodInfo tryGetMethod = typeInfo.Type.GetMethod("TryGetValue");
                if (tryGetMethod == null)
                {
                    return null;
                }

                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                ParameterExpression keyParam = Expression.Parameter(typeof(object), null);
                ParameterExpression outValueParam = Expression.Parameter(typeof(object).MakeByRefType(), null);
                Expression castTarget = ReflectionUtility.Convert(targetParam, typeInfo);
                Expression castKey = ReflectionUtility.Convert(keyParam, keyType);
                Expression callTryGet = Expression.Call(castTarget, tryGetMethod, castKey, outValueParam);

                return ReflectionUtility.CompileLambda<TryGetValueDelegate>(callTryGet, targetParam, keyParam, outValueParam);
            });

            this.callGet = new Lazy<Func<object, object, object>>(() =>
            {
                System.Reflection.MethodInfo getMethod = info.GetGetMethod(nonPublic: false);
                if (getMethod == null)
                {
                    return null;
                }

                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                ParameterExpression keyParam = Expression.Parameter(typeof(object), null);
                Expression castTarget = ReflectionUtility.Convert(targetParam, typeInfo);
                Expression castKey = ReflectionUtility.Convert(keyParam, keyType);
                Expression callGet = Expression.Call(castTarget, getMethod, castKey);

                return ReflectionUtility.CompileLambda<Func<object, object, object>>(callGet, targetParam, keyParam);
            });

            this.callSet = new Lazy<Action<object, object, object>>(() =>
            {
                System.Reflection.MethodInfo setMethod = info.GetSetMethod(nonPublic: false);
                if (setMethod == null)
                {
                    return null;
                }

                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                ParameterExpression keyParam = Expression.Parameter(typeof(object), null);
                ParameterExpression valueParam = Expression.Parameter(typeof(object), null);
                Expression castTarget = ReflectionUtility.Convert(targetParam, typeInfo);
                Expression castKey = ReflectionUtility.Convert(keyParam, keyType);
                Expression castValue = ReflectionUtility.Convert(valueParam, valueType);
                Expression callSet = Expression.Call(castTarget, setMethod, castKey, castValue);

                return ReflectionUtility.CompileLambda<Action<object, object, object>>(callSet, targetParam, keyParam, valueParam);
            });
        }

        protected override object GetMemberObject(object target, object key)
        {
            if (this.callTryGetValue.Value is TryGetValueDelegate tryGetValue)
            {
                return tryGetValue(target, key, out object value) ? value : null;
            }

            try
            {
                return this.callGet.Value(target, key);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        protected override void SetMemberObject(object target, object key, object value)
        {
            if (this.callSet.Value is Action<object, object, object> setValue)
            {
                setValue(target, key ?? this.GenerateKey(target), value);
            }
            else
            {
                throw JsonException.New(Resources.Convert_NoSetter, this.info.Name, this.info.DeclaringType.FullName);
            }
        }

        public static IEnumerator<KeyValuePair<object, object>> GetValuesFromDictionary(object instance, TypeCache typeCache)
        {
            Debug.Assert(instance == null || instance is IEnumerable);

            if (instance is IEnumerable enumerable)
            {
                Type lastType = null;
                TypeInfo lastTypeInfo = null;

                foreach (object value in enumerable)
                {
                    Debug.Assert(value != null && value.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>));

                    Type valueType = value?.GetType();
                    TypeInfo valueTypeInfo = null;

                    if (valueType != null)
                    {
                        if (valueType != lastType)
                        {
                            lastTypeInfo = typeCache.GetTypeInfo(value.GetType());
                        }

                        valueTypeInfo = lastTypeInfo;
                    }

                    yield return valueTypeInfo?.ToKeyValuePair(value) ?? default;
                }
            }
        }

        IEnumerator<KeyValuePair<object, object>> ICollectionInfo.GetValues(object instance)
        {
            return DictionaryInfo.GetValuesFromDictionary(instance, this.TypeInfo.TypeCache);
        }
    }
}
