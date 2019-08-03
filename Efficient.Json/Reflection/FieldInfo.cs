using System;
using System.Linq.Expressions;
using Efficient.Json.Utility;

namespace Efficient.Json.Reflection
{
    internal class FieldInfo : MemberInfo
    {
        public Lazy<Func<object, object>> callGet;
        public Lazy<Action<object, object>> callSet;

        public FieldInfo(TypeInfo typeInfo, System.Reflection.FieldInfo info, Type overrideType = null)
            : base(typeInfo, info, null, typeInfo.TypeCache.GetTypeInfo(overrideType ?? info.FieldType))
        {
            this.callGet = new Lazy<Func<object, object>>(() =>
            {
                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                Expression castTarget = ReflectionUtility.Convert(targetParam, typeInfo);
                Expression getField = Expression.Field(castTarget, info);
                Expression castField = Expression.Convert(getField, typeof(object));

                return ReflectionUtility.CompileLambda<Func<object, object>>(castField, targetParam);
            });

            this.callSet = new Lazy<Action<object, object>>(() =>
            {
                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                ParameterExpression valueParam = Expression.Parameter(typeof(object), null);
                Expression castTarget = ReflectionUtility.Convert(targetParam, typeInfo);
                Expression castValue = Expression.Convert(valueParam, info.FieldType);
                Expression accessField = Expression.Field(castTarget, info);
                Expression setField = Expression.Assign(accessField, castValue);

                return ReflectionUtility.CompileLambda<Action<object, object>>(setField, targetParam, valueParam);
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
