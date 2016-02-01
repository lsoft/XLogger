using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System.Collections.Concurrent
{
    /*

    //эта очередь используется в классе ThreadSafeLogger
    //но этот класс - ThreadSafeLogger - не нужен на Compact Framework
    //так как, на нашей МК только одно ядро и проще писать синхронно,
    //не создавая потоков записи (только лишние потери времени ЦП на переключение)
    //НЕ ИСПОЛЬЗУЙТЕ ThreadSafeLogger + ConcurrentQueue<T> НА COMPACT FRAMEWORK

    /// <summary>
    /// "Concurrent" queue for Compact Framework.
    /// There is neither Microsoft ConcurrentQueue, nor ReaderWriterLock* so
    /// Monitor-safety is an optimal choice.
    /// </summary>
    internal class ConcurrentQueue<T> : IEnumerable<T>, ICollection, IEnumerable
    {
        private readonly Queue<T> _queue;
        private readonly object _locker = new object();

        public int Count
        {
            get
            {
                lock (_locker)
                {
                    return
                        _queue.Count;
                }
            }
        }

        public ConcurrentQueue(
            )
        {
            _queue = new Queue<T>();
        }

        public ConcurrentQueue(
            int capacity
            )
        {
            _queue = new Queue<T>(capacity);
        }

        public ConcurrentQueue(IEnumerable<T> collection)
        {
            _queue = new Queue<T>(collection);
        }

        public void Clear()
        {
            lock (_locker)
            {
                _queue.Clear();
            }
        }

        public void Enqueue(T item)
        {
            lock (_locker)
            {
                _queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            lock (_locker)
            {
                return
                    _queue.Dequeue();
            }
        }

        public T Peek()
        {
            lock (_locker)
            {
                return
                    _queue.Peek();
            }
        }

        public bool TryDequeue(out T item)
        {
            var result = false;

            lock (_locker)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();

                    result = true;
                }
                else
                {
                    item = default (T);
                }
            }

            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException("Not implemented. Unused method for this assembly.");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException("Not implemented. Unused method for this assembly.");
        }

        public bool IsSynchronized
        {
            get
            {
                throw new NotImplementedException("Not implemented. Unused method for this assembly.");
            }
        }

        public object SyncRoot
        {
            get
            {
                throw new NotImplementedException("Not implemented. Unused method for this assembly.");
            }
        }
    }

    //*/
}
