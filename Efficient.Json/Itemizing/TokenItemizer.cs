using Efficient.Json.Tokenizing;
using Efficient.Json.Tokens;
using Efficient.Json.Utility;
using System.Collections.Generic;

namespace Efficient.Json.Itemizing
{
    internal class TokenItemizer : Itemizer
    {
        private readonly Tokenizer tokenizer;
        private readonly Stack<OpenItem> openItems;
        private OpenItem openItem;
        private State state;
        private FullToken token;

        private enum State { None, Value, Key, KeyColon, Separator }
        private enum OpenItem { None, Array, Object }

        public TokenItemizer(Tokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
            this.openItems = new Stack<OpenItem>(Constants.BufferSize);
            this.state = State.Value;
        }

        public override FullToken ValueToken => this.token;
        private FullToken NextToken() => this.tokenizer.NextToken();

        public override Item NextItem()
        {
            Item item = default;

            while (item.Type == ItemType.None)
            {
                this.token = this.NextToken();

                switch (this.state)
                {
                    case State.Value:
                        switch (this.token.Type)
                        {
                            case TokenType.OpenBracket:
                                this.openItems.Push(this.openItem);
                                this.openItem = OpenItem.Array;
                                item = new Item(ItemType.ArrayStart);
                                break;

                            case TokenType.CloseBracket:
                                item = new Item(ItemType.ArrayEnd);
                                this.openItem = this.openItems.Pop();
                                this.state = (this.openItem == OpenItem.None) ? State.None : State.Separator;
                                break;

                            case TokenType.OpenCurly:
                                this.openItems.Push(this.openItem);
                                this.openItem = OpenItem.Object;
                                this.state = State.Key;
                                item = new Item(ItemType.ObjectStart);
                                break;

                            default:
                                if (this.token.HasType(TokenType.AnyValue))
                                {
                                    item = new Item(ItemType.Value);
                                    this.state = (this.openItem == OpenItem.None) ? State.None : State.Separator;
                                }
                                else
                                {
                                    throw this.ParseException(this.token, Resources.Parser_ExpectedValue);
                                }
                                break;
                        }
                        break;

                    case State.Key:
                        switch (this.token.Type)
                        {
                            case TokenType.String:
                            case TokenType.EncodedString:
                                item = new Item(ItemType.Key);
                                this.state = State.KeyColon;
                                break;

                            case TokenType.CloseCurly:
                                item = new Item(ItemType.ObjectEnd);
                                this.openItem = this.openItems.Pop();
                                this.state = (this.openItem == OpenItem.None) ? State.None : State.Separator;
                                break;

                            default:
                                throw this.ParseException(this.token, Resources.Parser_ExpectedKeyName);
                        }
                        break;

                    case State.KeyColon:
                        switch (this.token.Type)
                        {
                            case TokenType.Colon:
                                this.state = State.Value;
                                break;

                            default:
                                throw this.ParseException(this.token, Resources.Parser_ExpectedKeyName);
                        }
                        break;

                    case State.Separator:
                        switch (this.openItem)
                        {
                            case OpenItem.Array:
                                switch (this.token.Type)
                                {
                                    case TokenType.Comma:
                                        this.state = State.Value;
                                        break;

                                    case TokenType.CloseBracket:
                                        item = new Item(ItemType.ArrayEnd);
                                        this.openItem = this.openItems.Pop();
                                        this.state = (this.openItem == OpenItem.None) ? State.None : State.Separator;
                                        break;

                                    default:
                                        throw this.ParseException(this.token, Resources.Parser_ExpectedCommaOrBracket);
                                }
                                break;

                            default: // must be OpenItem.Object
                                switch (this.token.Type)
                                {
                                    case TokenType.Comma:
                                        this.state = State.Key;
                                        break;

                                    case TokenType.CloseCurly:
                                        item = new Item(ItemType.ObjectEnd);
                                        this.openItem = this.openItems.Pop();
                                        this.state = (this.openItem == OpenItem.None) ? State.None : State.Separator;
                                        break;

                                    default:
                                        throw this.ParseException(this.token, Resources.Parser_ExpectedCommaOrCurly);
                                }
                                break;
                        }
                        break;

                    case State.None:
                        switch (this.token.Type)
                        {
                            case TokenType.None:
                                item = new Item(ItemType.End);
                                break;

                            default:
                                throw this.ParseException(this.token, Resources.Parser_UnexpectedValue);
                        }
                        break;
                }
            }

            return item;
        }
    }
}
