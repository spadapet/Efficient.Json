using System;

namespace Efficient.Json.Value
{
    /// <summary>
    /// Dynamic binding in ValueDynamic will use this interface to call into JsonValue
    /// </summary>
    internal interface IValueDynamic
    {
        object Convert(Type type);
        object GetIndex(object[] indexes);
        object GetMember(string name);
    }
}
