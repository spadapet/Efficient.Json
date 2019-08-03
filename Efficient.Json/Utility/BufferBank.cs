using System;
using System.Collections.Generic;

namespace Efficient.Json.Utility
{
    internal class BufferBank<T>
    {
        private readonly Stack<List<T>> buffers;

        public BufferBank()
        {
            this.buffers = new Stack<List<T>>(Constants.BufferSize);
        }

        public List<T> Borrow(int capacity = 0)
        {
            List<T> buffer;

            if (this.buffers.Count > 0)
            {
                buffer = this.buffers.Pop();

                if (capacity > buffer.Capacity)
                {
                    buffer.Capacity = capacity;
                }
            }
            else
            {
                buffer = new List<T>(Math.Max(Constants.BufferSize, capacity));
            }

            return buffer;
        }

        public void Return(List<T> buffer)
        {
            if (buffer != null)
            {
                buffer.Clear();
                this.buffers.Push(buffer);
            }
        }
    }
}
