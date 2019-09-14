using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Efficient.Json.Itemizing;
using Efficient.Json.Utility;

namespace Efficient.Json.Serializing
{
    internal class TextSerializer
    {
        public static void Serialize(Itemizer itemizer, string indent, TextWriter writer)
        {
            TextSerializer serializer = new TextSerializer(itemizer, indent, writer);
            serializer.Serialize(itemizer.NextItem());
        }

        private readonly Itemizer itemizer;
        private readonly string indent;
        private readonly StringBuilder indentBuffer;
        private string currentIndent;
        private readonly Stack<State> states;
        private readonly TextWriter writer;
        private State state;
        private bool PrettyPrint => this.indent.Length != 0;

        private enum StateType { Root, Array, Object }

        private struct State
        {
            public int childCount;
            public StateType type;
        }

        private TextSerializer(Itemizer itemizer, string indent, TextWriter writer)
        {
            this.itemizer = itemizer;
            this.indent = indent ?? string.Empty;
            this.currentIndent = string.Empty;
            this.indentBuffer = new StringBuilder(this.indent.Length != 0 ? Environment.NewLine : string.Empty);
            this.states = new Stack<State>(Constants.BufferSize);
            this.writer = writer;
        }

        public void Serialize(Item item)
        {
            for (; item.Type != ItemType.End; item = this.itemizer.NextItem())
            {
                switch (item.Type)
                {
                    case ItemType.ArrayStart:
                    case ItemType.ObjectStart:
                        this.WriteSeparator(item);
                        this.writer.Write((item.Type == ItemType.ArrayStart) ? '[' : '{');
                        this.PushIndent();

                        this.state.childCount++;
                        this.states.Push(this.state);
                        this.state.childCount = 0;
                        this.state.type = (item.Type == ItemType.ArrayStart) ? StateType.Array : StateType.Object;
                        break;

                    case ItemType.ArrayEnd:
                    case ItemType.ObjectEnd:
                        this.PopIndent();
                        this.WriteIndent(item);
                        this.writer.Write((item.Type == ItemType.ArrayEnd) ? ']' : '}');
                        this.state = this.states.Pop();
                        break;

                    case ItemType.Key:
                        this.WriteSeparator(item);
                        EncodingUtility.EncodeTokenAsString(this.itemizer.ValueToken, this.writer);
                        this.writer.Write(':');
                        break;

                    case ItemType.Value:
                        this.WriteSeparator(item);
                        EncodingUtility.EncodeToken(this.itemizer.ValueToken, this.writer);
                        this.state.childCount++;
                        break;
                }
            }
        }

        private void PushIndent()
        {
            this.indentBuffer.Append(this.indent);
            this.currentIndent = this.indentBuffer.ToString();
        }

        private void PopIndent()
        {
            this.indentBuffer.Remove(this.indentBuffer.Length - this.indent.Length, this.indent.Length);
            this.currentIndent = this.indentBuffer.ToString();
        }

        private void WriteSeparator(Item item)
        {
            if (this.state.type == StateType.Object && item.Type != ItemType.Key)
            {
                if (this.PrettyPrint)
                {
                    this.writer.Write(' ');
                }
            }
            else if (this.state.childCount != 0)
            {
                this.writer.Write(',');
            }

            this.WriteIndent(item);
        }

        private void WriteIndent(Item item)
        {
            if (this.PrettyPrint && (this.state.type != StateType.Object || item.Type != ItemType.Value))
            {
                this.writer.Write(this.currentIndent);
            }
        }
    }
}
