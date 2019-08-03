using System.Collections.Generic;

namespace Efficient.Json.Reflection
{
    internal interface ICollectionInfo
    {
        bool SerializeAsObject { get; }
        MemberInfo AsMember { get; }

        IEnumerator<KeyValuePair<object, object>> GetValues(object instance);
    }
}
