using System;
using System.Linq.Expressions;
using Efficient.Json.Utility;

namespace Efficient.Json.Reflection
{
    /// <summary>
    /// Caches reflection info about one property
    /// </summary>
    internal class PropertyInfo : MemberInfo
    {
        public Lazy<Func<object, object>> callGet;
        public Lazy<Action<object, object>> callSet;

        public PropertyInfo(TypeInfo typeInfo, System.Reflection.PropertyInfo info)
            : base(typeInfo, info, null, typeInfo.TypeCache.GetTypeInfo(info.PropertyType))
        {
            this.callGet = new Lazy<Func<object, object>>(() =>
            {
                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                Expression castTarget = ReflectionUtility.Convert(targetParam, typeInfo);
                MemberExpression propertyAccess = Expression.MakeMemberAccess(castTarget, info);
                Expression castProperty = Expression.Convert(propertyAccess, typeof(object));

                return ReflectionUtility.CompileLambda<Func<object, object>>(castProperty, targetParam);
            });

            this.callSet = new Lazy<Action<object, object>>(() =>
            {
                System.Reflection.MethodInfo setMethod = info.GetSetMethod(nonPublic: true);
                if (setMethod == null)
                {
                    throw JsonException.New(Resources.Convert_NoSetter, info.Name, info.DeclaringType.FullName);
                }

                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                ParameterExpression valueParam = Expression.Parameter(typeof(object), null);
                Expression castTarget = ReflectionUtility.Convert(targetParam, typeInfo);
                Expression castValue = Expression.Convert(valueParam, info.PropertyType);
                Expression propertySet = Expression.Call(castTarget, setMethod, castValue);

                return ReflectionUtility.CompileLambda<Action<object, object>>(propertySet, targetParam, valueParam);
            });
        }

        protected override object GetMemberObject(object target, object key)
        {
            return this.callGet.Value(target);
        }

        protected override void SetMemberObject(object target, object key, object value)
        {
            this.callSet.Value(target, value);
        }
    }
}
