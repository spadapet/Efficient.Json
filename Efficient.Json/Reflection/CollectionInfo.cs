using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Efficient.Json.Utility;

namespace Efficient.Json.Reflection
{
    internal class CollectionInfo : MemberInfo, ICollectionInfo
    {
        public static ICollectionInfo TryCreate(TypeInfo typeInfo, System.Reflection.PropertyInfo info, System.Reflection.MethodInfo addMethod, System.Reflection.ParameterInfo[] addMethodParameters)
        {
            if (addMethodParameters.Length == 1)
            {
                TypeInfo valueType = typeInfo.TypeCache.GetTypeInfo(addMethodParameters[0].ParameterType);
                return new CollectionInfo(typeInfo, info, addMethod, valueType);
            }

            return null;
        }

        bool ICollectionInfo.SerializeAsObject => false;
        MemberInfo ICollectionInfo.AsMember => this;
        private readonly Lazy<Action<object, object>> callAdd;

        private CollectionInfo(TypeInfo typeInfo, System.Reflection.PropertyInfo info, System.Reflection.MethodInfo addMethod, TypeInfo valueType)
            : base(typeInfo, info, null, valueType)
        {
            this.callAdd = new Lazy<Action<object, object>>(() =>
            {
                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                ParameterExpression valueParam = Expression.Parameter(typeof(object), null);
                Expression castTarget = ReflectionUtility.Convert(targetParam, typeInfo);
                Expression castValue = ReflectionUtility.Convert(valueParam, valueType);
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
            this.callAdd.Value(target, value);
        }

        public static IEnumerator<KeyValuePair<object, object>> GetValuesFromEnumerable(object instance)
        {
            Debug.Assert(instance == null || instance is IEnumerable);

            if (instance is IEnumerable enumerable)
            {
                foreach (object value in enumerable)
                {
                    yield return new KeyValuePair<object, object>(null, value);
                }
            }
        }

        IEnumerator<KeyValuePair<object, object>> ICollectionInfo.GetValues(object instance)
        {
            return CollectionInfo.GetValuesFromEnumerable(instance);
        }
    }
}
