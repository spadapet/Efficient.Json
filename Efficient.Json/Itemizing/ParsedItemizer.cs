using System.Collections.Generic;
using Efficient.Json.Tokens;
using Efficient.Json.Utility;

namespace Efficient.Json.Itemizing
{
    internal class ParsedItemizer : Itemizer
    {
        private FullToken token;
        private readonly Stack<State> states;
        private State state;

        private enum StateType { RootValue, ArrayValue, ObjectKey, ObjectValue }

        private struct State
        {
            public JsonValue value;
            public int index;
            public StateType type;

        }

        public ParsedItemizer(JsonValue value)
        {
            this.token = FullToken.Empty;
            this.states = new Stack<State>(Constants.BufferSize);
            this.state = new State() { value = value };
        }

        public override FullToken ValueToken => this.token;

        public override Item NextItem()
        {
            Item item = default;
            JsonValue value = null;

            switch (this.state.type)
            {
                case StateType.RootValue:
                    if (this.state.index == 0)
                    {
                        value = this.state.value;
                    }
                    else
                    {
                        item = new Item(ItemType.End);
                    }
                    break;

                case StateType.ArrayValue:
                    if (this.state.index < this.state.value.InternalArray.Length)
                    {
                        value = this.state.value.InternalArray[this.state.index];
                    }
                    else
                    {
                        item = new Item(ItemType.ArrayEnd);
                        this.state = this.states.Pop();
                    }
                    break;

                case StateType.ObjectValue:
                    value = this.state.value.InternalArray[this.state.index];
                    this.state.type = StateType.ObjectKey;
                    break;

                case StateType.ObjectKey:
                    if (this.state.index < this.state.value.InternalKeys.Length)
                    {
                        item = new Item(ItemType.Key);
                        this.token = this.state.value.InternalKeys[this.state.index].FullToken;
                        this.state.type = StateType.ObjectValue;
                    }
                    else
                    {
                        item = new Item(ItemType.ObjectEnd);
                        this.state = this.states.Pop();
                    }
                    break;
            }

            if (value != null)
            {
                this.state.index++;

                switch (value.Type)
                {
                    case TokenType.ArrayValue:
                    case TokenType.ObjectValue:
                        bool isArrayValue = (value.Type == TokenType.ArrayValue);
                        item = new Item(isArrayValue ? ItemType.ArrayStart : ItemType.ObjectStart, value.InternalArray.Length);
                        this.states.Push(this.state);
                        this.state.value = value;
                        this.state.index = 0;
                        this.state.type = isArrayValue ? StateType.ArrayValue : StateType.ObjectKey;
                        break;

                    default:
                        item = new Item(ItemType.Value, this.state.value.Value);
                        this.token = value.FullToken;
                        break;
                }
            }

            return item;
        }
    }
}
