using Efficient.Json.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Efficient.Json.Reflection
{
    internal class TypeReflection
    {
        public Dictionary<string, MemberInfo> NameToMember { get; private set; }
        public List<MemberInfo> Members { get; private set; }
        public ICollectionInfo CollectionInfo { get; private set; }

        public Lazy<Action<object, StreamingContext>> DeserializingMethod { get; private set; }
        public Lazy<Action<object, StreamingContext>> DeserializedMethod { get; private set; }
        public Lazy<Action<object, StreamingContext>> SerializingMethod { get; private set; }
        public Lazy<Action<object, StreamingContext>> SerializedMethod { get; private set; }

        public Lazy<Func<object, Array>> ToArrayMethod { get; private set; }
        public Lazy<Func<object, KeyValuePair<object, object>>> ToKeyValuePairMethod { get; private set; }
        public Lazy<Func<object>> DefaultConstructor { get; private set; }
        public Lazy<Func<int, object>> CapacityConstructor { get; private set; }
        public Lazy<Func<object, bool>> IsValueTypeDefault { get; private set; }

        public TypeReflection(TypeInfo typeInfo)
        {
            if (typeInfo.IsClass || typeInfo.IsValueType)
            {
                if (!typeInfo.IsAbstract)
                {
                    this.InitConstructors(typeInfo);
                }

                System.Reflection.PropertyInfo defaultProperty = typeInfo.Type.GetDefaultMembers().OfType<System.Reflection.PropertyInfo>().FirstOrDefault();
                if (defaultProperty != null)
                {
                    this.InitCollection(typeInfo, defaultProperty);
                }
                else
                {
                    this.InitMembers(typeInfo);
                }

                if (typeInfo.HasDataContract)
                {
                    this.InitDataContract(typeInfo);
                }

                if (typeInfo.IsValueType)
                {
                    this.InitValueTypeDefault(typeInfo);
                }
            }
        }

        private void InitConstructors(TypeInfo typeInfo)
        {
            Type type = typeInfo.Type;

            if (type.GetConstructor(Type.EmptyTypes) is System.Reflection.ConstructorInfo defaultConstructor)
            {
                this.DefaultConstructor = new Lazy<Func<object>>(() =>
                {
                    Expression createObj = typeInfo.IsValueType ? Expression.New(type) : Expression.New(defaultConstructor);
                    Expression convertObj = typeInfo.IsValueType ? Expression.Convert(createObj, typeof(object)) : createObj;
                    return ReflectionUtility.CompileLambda<Func<object>>(convertObj);
                });
            }

            if (typeInfo.IsArray || typeInfo.GenericTypeDefinition == typeof(List<>) || typeInfo.GenericTypeDefinition == typeof(Dictionary<,>))
            {
                if (type.GetConstructor(new Type[] { typeof(int) }) is System.Reflection.ConstructorInfo capacityConstructor)
                {
                    this.CapacityConstructor = new Lazy<Func<int, object>>(() =>
                    {
                        ParameterExpression capacityParam = Expression.Parameter(typeof(int), null);
                        Expression createObj = Expression.New(capacityConstructor, capacityParam);
                        Expression convertObj = typeInfo.IsValueType ? Expression.Convert(createObj, typeof(object)) : createObj;
                        return ReflectionUtility.CompileLambda<Func<int, object>>(convertObj, capacityParam);
                    });
                }
            }

            if (typeInfo.GenericTypeDefinition == typeof(List<>) && type.GetMethod("ToArray") is System.Reflection.MethodInfo toArrayMethod)
            {
                this.ToArrayMethod = new Lazy<Func<object, Array>>(() =>
                {
                    ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                    Expression targetCast = Expression.Convert(targetParam, toArrayMethod.DeclaringType);
                    Expression callToArray = Expression.Call(targetCast, toArrayMethod);
                    Expression resultCast = Expression.Convert(callToArray, typeof(Array));
                    return ReflectionUtility.CompileLambda<Func<object, Array>>(resultCast, targetParam);
                });
            }

            if (typeInfo.GenericTypeDefinition == typeof(KeyValuePair<,>) &&
                type.GetMethod("get_Key") is System.Reflection.MethodInfo getKeyMethod &&
                type.GetMethod("get_Value") is System.Reflection.MethodInfo getValueMethod)
            {
                Type kvpType = typeof(KeyValuePair<object, object>);
                Type[] args = type.GetGenericArguments();

                this.ToKeyValuePairMethod = new Lazy<Func<object, KeyValuePair<object, object>>>(() =>
                {
                    ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                    Expression targetCast = Expression.Convert(targetParam, type);
                    Expression keyCast = Expression.Convert(Expression.Call(targetCast, getKeyMethod), typeof(object));
                    Expression valueCast = Expression.Convert(Expression.Call(targetCast, getValueMethod), typeof(object));
                    Expression newKvp = Expression.New(kvpType.GetConstructor(new Type[] { typeof(object), typeof(object) }), keyCast, valueCast);

                    Expression result = Expression.Condition(
                        Expression.ReferenceEqual(targetParam, Expression.Constant(null)),
                        Expression.Default(kvpType),
                        newKvp, kvpType);

                    return ReflectionUtility.CompileLambda<Func<object, KeyValuePair<object, object>>>(result, targetParam);
                });
            }
        }

        private void InitCollection(TypeInfo typeInfo, System.Reflection.PropertyInfo defaultProperty)
        {
            System.Reflection.MethodInfo addMethod = typeInfo.Type.GetMethod("Add");
            System.Reflection.ParameterInfo[] addParameters = addMethod?.GetParameters();

            if (addParameters != null)
            {
                this.CollectionInfo =
                    Reflection.DictionaryInfo.TryCreate(typeInfo, defaultProperty, addParameters) ??
                    Reflection.KvpCollectionInfo.TryCreate(typeInfo, defaultProperty, addMethod, addParameters) ??
                    Reflection.CollectionInfo.TryCreate(typeInfo, defaultProperty, addMethod, addParameters);
            }
        }

        private void InitMembers(TypeInfo typeInfo)
        {
            IReadOnlyList<MemberInfo> baseMembers = typeInfo.BaseTypeInfo?.Members ?? Array.Empty<MemberInfo>();
            List<MemberInfo> newMembers = typeInfo.Type.FindMembers(
                System.Reflection.MemberTypes.Field | System.Reflection.MemberTypes.Property,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly,
                (i, o) => true, null).Select(i => this.CreateMemberInfo(typeInfo, i)).Where(m => m != null).ToList();
            newMembers.Sort();

            List<MemberInfo> allMembers = new List<MemberInfo>(baseMembers.Count + newMembers.Count);
            Dictionary<string, MemberInfo> allMembersDict = new Dictionary<string, MemberInfo>(baseMembers.Count + newMembers.Count);

            for (int i = newMembers.Count - 1; i >= 0; i--)
            {
                MemberInfo mi = newMembers[i];
                if (!allMembersDict.ContainsKey(mi.Name))
                {
                    allMembersDict.Add(mi.Name, mi);
                    allMembers.Add(mi);
                }
            }

            for (int i = baseMembers.Count - 1; i >= 0; i--)
            {
                MemberInfo mi = baseMembers[i];
                if (!allMembersDict.ContainsKey(mi.Name))
                {
                    allMembersDict.Add(mi.Name, mi);
                    allMembers.Add(mi);
                }
            }

            allMembers.Reverse();
            this.Members = allMembers;
            this.NameToMember = allMembersDict;
        }

        private void InitDataContract(TypeInfo typeInfo)
        {
            foreach (System.Reflection.MethodInfo methodInfo in typeInfo.Type.GetMethods(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly))
            {
                if (methodInfo.IsSpecialName)
                {
                    continue;
                }

                foreach (System.Reflection.CustomAttributeData data in methodInfo.CustomAttributes)
                {
                    if (this.DeserializingMethod == null && typeof(OnDeserializingAttribute).IsAssignableFrom(data.AttributeType))
                    {
                        this.DeserializingMethod = this.CreateStreamingContextCall(typeInfo, methodInfo);
                    }

                    if (this.DeserializedMethod == null && typeof(OnDeserializedAttribute).IsAssignableFrom(data.AttributeType))
                    {
                        this.DeserializedMethod = this.CreateStreamingContextCall(typeInfo, methodInfo);
                    }

                    if (this.SerializingMethod == null && typeof(OnSerializingAttribute).IsAssignableFrom(data.AttributeType))
                    {
                        this.SerializingMethod = this.CreateStreamingContextCall(typeInfo, methodInfo);
                    }

                    if (this.SerializedMethod == null && typeof(OnSerializedAttribute).IsAssignableFrom(data.AttributeType))
                    {
                        this.SerializedMethod = this.CreateStreamingContextCall(typeInfo, methodInfo);
                    }
                }
            }
        }

        private void InitValueTypeDefault(TypeInfo typeInfo)
        {
            this.IsValueTypeDefault = new Lazy<Func<object, bool>>(() =>
            {
                ParameterExpression valueParam = Expression.Parameter(typeof(object), null);
                Expression defaultValue = Expression.Default(typeInfo.Type);
                Expression defaultObject = Expression.Convert(defaultValue, typeof(object));
                Expression compare = Expression.Equal(valueParam, defaultObject);
                return ReflectionUtility.CompileLambda<Func<object, bool>>(compare, valueParam);
            });
        }

        private Lazy<Action<object, StreamingContext>> CreateStreamingContextCall(TypeInfo typeInfo, System.Reflection.MethodInfo methodInfo)
        {
            return new Lazy<Action<object, StreamingContext>>(() =>
            {
                ParameterExpression targetParam = Expression.Parameter(typeof(object), null);
                ParameterExpression contextParam = Expression.Parameter(typeof(StreamingContext), null);
                Expression castTarget = Expression.Convert(targetParam, typeInfo.Type);
                Expression callMethod = Expression.Call(castTarget, methodInfo, contextParam);
                return ReflectionUtility.CompileLambda<Action<object, StreamingContext>>(callMethod, targetParam, contextParam);
            });
        }

        private MemberInfo CreateMemberInfo(TypeInfo typeInfo, System.Reflection.MemberInfo info)
        {
            MemberInfo cachedInfo;

            if (info is System.Reflection.FieldInfo fieldInfo)
            {
                cachedInfo = new FieldInfo(typeInfo, fieldInfo);
            }
            else if (info is System.Reflection.PropertyInfo propertyInfo)
            {
                cachedInfo = new PropertyInfo(typeInfo, propertyInfo);
            }
            else
            {
                return null;
            }

            return !string.IsNullOrEmpty(cachedInfo.Name) ? cachedInfo : null;
        }
    }
}
