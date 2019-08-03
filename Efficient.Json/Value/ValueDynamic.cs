using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Efficient.Json.Value
{
    /// <summary>
    /// Allows a JsonValue to be treated as a .NET dynamic object
    /// </summary>
    internal class ValueDynamic : DynamicMetaObject
    {
        private delegate DynamicMetaObject Fallback(DynamicMetaObject errorSuggestion);

        public ValueDynamic(JsonValue value, Expression parameter)
            : base(parameter, BindingRestrictions.Empty, value)
        {
        }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            MethodInfo method = typeof(IValueDynamic).GetMethod(nameof(IValueDynamic.Convert));
            Expression instance = Expression.Convert(this.Expression, typeof(IValueDynamic));
            Expression typeConstant = Expression.Constant(binder.Type, typeof(Type));
            Expression methodCall = Expression.Call(instance, method, typeConstant);
            Expression convertCall = Expression.Convert(methodCall, binder.ReturnType);
            return this.BindResult(convertCall);
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            MethodInfo method = typeof(IValueDynamic).GetMethod(nameof(IValueDynamic.GetIndex));
            Expression instance = Expression.Convert(this.Expression, typeof(IValueDynamic));
            Expression[] indexParams = indexes.Select(i => Expression.Convert(i.Expression, typeof(object))).ToArray();
            Expression indexArray = Expression.NewArrayInit(typeof(object), indexParams);
            Expression methodCall = Expression.Call(instance, method, indexArray);
            Expression convertCall = Expression.Convert(methodCall, binder.ReturnType);
            return this.BindResult(convertCall);
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            MethodInfo method = typeof(IValueDynamic).GetMethod(nameof(IValueDynamic.GetMember));
            Expression instance = Expression.Convert(this.Expression, typeof(IValueDynamic));
            Expression memberConstant = Expression.Constant(binder.Name, typeof(string));
            Expression methodCall = Expression.Call(instance, method, memberConstant);
            Expression convertCall = Expression.Convert(methodCall, binder.ReturnType);
            return this.BindResult(convertCall);
        }

        private DynamicMetaObject BindResult(Expression expression)
        {
            return new DynamicMetaObject(expression, BindingRestrictions.GetTypeRestriction(this.Expression, this.LimitType));
        }
    }
}
