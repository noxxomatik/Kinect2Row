using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    /// <summary>
    /// A FIFO buffer which overwrites old values if the size is reached.
    /// </summary>
    public class CircularBuffer
    {
        private Queue queue;
        private int limit;

        public CircularBuffer(int size)
        {
            limit = size;
            queue = new Queue(size);
        }

        public void Enqueue(object obj)
        {
            if (queue.Count == limit) {
                queue.Dequeue();                
            }
            queue.Enqueue(obj);
            queue.TrimToSize();
        }

        public object[] ToArray()
        {
            return queue.ToArray();
        }
    }
}
