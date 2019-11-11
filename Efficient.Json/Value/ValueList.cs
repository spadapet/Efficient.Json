using Efficient.Json.Tokens;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Efficient.Json.Value
{
    /// <summary>
    /// Stores child JsonValues in either an array or dictionary after parsing them
    /// </summary>
    [DebuggerTypeProxy(typeof(DebuggerView))]
    internal class ValueList : IReadOnlyList<JsonValue>, IReadOnlyDictionary<string, JsonValue>
    {
        public JsonContext Context { get; }
        public Key[] Keys { get; }
        public JsonValue[] Values { get; }

        public ValueList(List<JsonValue> values, JsonContext context)
        {
            this.Keys = System.Array.Empty<Key>();
            this.Values = values.ToArray();
            this.Context = context;
        }

        public ValueList(List<Key> keys, List<JsonValue> values, JsonContext context)
        {
            this.Keys = keys.ToArray();
            this.Values = values.ToArray();
            this.Context = context;
        }

        private bool IsArray => object.ReferenceEquals(this.Keys, System.Array.Empty<Key>());
        public bool IsEmpty => this.Values.Length == 0;
        public IReadOnlyList<JsonValue> Array => this;
        public IReadOnlyDictionary<string, JsonValue> Dictionary => this;

        JsonValue IReadOnlyList<JsonValue>.this[int index] => (index >= 0 && index < this.Values.Length) ? this.Values[index] : this.Context.InvalidValue;
        int IReadOnlyCollection<JsonValue>.Count => this.Values.Length;
        IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator() => ((IList<JsonValue>)this.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.IsArray ? (IEnumerator)this.Array.GetEnumerator() : (IEnumerator)this.Dictionary.GetEnumerator();

        int IReadOnlyCollection<KeyValuePair<string, JsonValue>>.Count => this.Keys.Length;
        IEnumerable<string> IReadOnlyDictionary<string, JsonValue>.Keys => this.Keys.Select(k => k.Text);
        IEnumerable<JsonValue> IReadOnlyDictionary<string, JsonValue>.Values => this.Values;
        bool IReadOnlyDictionary<string, JsonValue>.ContainsKey(string key) => this.IndexOfKey(key) != -1;

        IEnumerator<KeyValuePair<string, JsonValue>> IEnumerable<KeyValuePair<string, JsonValue>>.GetEnumerator()
        {
            for (int i = 0; i < this.Keys.Length; i++)
            {
                yield return new KeyValuePair<string, JsonValue>(this.Keys[i].Text, this.Values[i]);
            }
        }

        bool IReadOnlyDictionary<string, JsonValue>.TryGetValue(string key, out JsonValue value)
        {
            int i = this.IndexOfKey(key);
            value = (i != -1) ? this.Values[i] : this.Context.InvalidValue;
            return i != -1;
        }

        JsonValue IReadOnlyDictionary<string, JsonValue>.this[string key]
        {
            get
            {
                int i = this.IndexOfKey(key);
                return (i != -1) ? this.Values[i] : this.Context.InvalidValue;
            }
        }

        private int IndexOfKey(string key)
        {
            if (key != null)
            {
                Key jsonKey = new Key(key);

                for (int i = 0; i < this.Keys.Length; i++)
                {
                    if (KeyComparer.AreEqual(jsonKey, this.Keys[i]))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public override string ToString()
        {
            return this.IsArray
                ? $"Array, Count={this.Array.Count}"
                : $"Dictionary, Count={this.Dictionary.Count}";
        }

        private class DebuggerView
        {
            private readonly ValueList values;

            public DebuggerView(ValueList values)
            {
                this.values = values;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public IEnumerable Values => this.values.IsArray
                ? (IEnumerable)this.values.Array.ToArray()
                : (IEnumerable)this.values.Dictionary.ToArray();
        }
    }
}
