using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive
{
    internal class ThreadSafeList<T>
    {
        protected readonly List<T> _list;
        protected readonly object _lock;

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _list.Count;
                }
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    return _list[index];
                }
            }
            set
            {
                lock (_lock)
                {
                    _list[index] = value;
                }
            }
        }

        public ThreadSafeList()
        {
            _list = new List<T>();
            _lock = new object();
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                if (index >= 0 && index < _list.Count)
                {
                    _list.RemoveAt(index);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
                }
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                return _list.Remove(item);
            }
        }

        public bool Any(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                return _list.Any(predicate);
            }
        }

        public void RemoveAll(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                _list.RemoveAll(new Predicate<T>(predicate));
            }
        }

        public T First(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                return _list.First(predicate);
            }
        }

        public List<T> FindAll(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                return _list.FindAll(new Predicate<T>(predicate));
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _list.Contains(item);
            }
        }

        public bool Contains(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                return _list.Any(predicate);
            }
        }
        public IEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            lock (_lock)
            {
                return _list.Select(selector).ToList();
            }
        }
        
    }
}
