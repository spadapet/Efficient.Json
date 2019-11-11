using Efficient.Json.Reflection;
using System.Linq.Expressions;

namespace Efficient.Json.Utility
{
    internal static class ReflectionUtility
    {
        public static DelegateT CompileLambda<DelegateT>(Expression body, params ParameterExpression[] parameters) where DelegateT : System.Delegate
        {
            LambdaExpression compile = Expression.Lambda(typeof(DelegateT), body, parameters);
            return (DelegateT)compile.Compile();
        }

        public static Expression Convert(Expression value, TypeInfo typeInfo)
        {
            return typeInfo.IsValueType
                ? Expression.Unbox(value, typeInfo.Type)
                : Expression.Convert(value, typeInfo.Type);
        }
    }
}
