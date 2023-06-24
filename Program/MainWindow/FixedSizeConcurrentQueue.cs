using System.Collections.Concurrent;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// A concurrent queue with a size limit.
    /// </summary>
    public class FixedSizeConcurrentQueue<T> : ConcurrentQueue<T>
    {
        private readonly object queueLock = new object();

        /// <summary>
        /// Size of the queue.
        /// </summary>
        public int MaxSize { get; private set; }

        /// <summary>
        /// Creates a new queue with the given max size.
        /// </summary>

        public FixedSizeConcurrentQueue(int size)
        {
            MaxSize = size;
        }

        /// <summary>
        /// Enqueues an object into the queue, discarding any items at the end of the queue which exceed the max count.
        /// </summary>
        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (queueLock)
            {
                while (Count > MaxSize)
                {
                    TryDequeue(out _);
                }
            }
        }
    }
}
