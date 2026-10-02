using System;
using System.Collections.Generic;

namespace gSDK
{
    public class ReactiveList<T> : Reactive<List<T>>
    {
        public int Count => _value.Count;
        

        public ReactiveList() : base(new())
        { }

        public ReactiveList(List<T> list) : base(list)
        { }

        public void Add(T item)
        {
            _value.Add(item);
            Notify();
        }

        public void AddRange(IEnumerable<T> collection)
        {
            _value.AddRange(collection);
            Notify();
        }

        public bool AddIfMissing(T item)
        {
            bool success = _value.AddIfMissing(item);

            if (success)
            {
                Notify();
            }

            return success;
        }

        public void Clear()
        {
            _value.Clear();
            Notify();
        }

        public void Insert(int index, T item)
        {
            _value.Insert(index, item);
            Notify();
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            _value.InsertRange(index, collection);
            Notify();
        }

        public T Find(Predicate<T> match)
        {
            return _value.Find(match);
        }

        public bool TryFind(Predicate<T> match, out T result)
        {
            if (Exists(match))
            {
                result = Find(match);
                return true;
            }
            result = default;
            return false;
        }

        public List<T> FindAll(Predicate<T> match)
        {
            return _value.FindAll(match);
        }

        public int FindIndex(Predicate<T> match)
        {
            return _value.FindIndex(match);
        }

        public int IndexOf(T item)
        {
            return _value.IndexOf(item);
        }

        public bool Contains(T item)
        {
            return _value.Contains(item);
        }

        public bool Exists(Predicate<T> match)
        {
            return _value.Exists(match);
        }

        public bool TryRemove(Predicate<T> match)
        {
            if (_value.TryRemove(match))
            {
                Notify();
                return true;
            }
            return false;
        }

        public void Remove(T item)
        {
            _value.Remove(item);
            Notify();
        }

        public int RemoveAll(Predicate<T> match)
        {
            int count = _value.RemoveAll(match);
            if (count > 0)
            {
                Notify();
            }
            return count;
        }

        public void RemoveAt(int index)
        {
            _value.RemoveAt(index);
            Notify();
        }

        public void RemoveRange(int index, int count)
        {
            _value.RemoveRange(index, count);
            Notify();
        }

        public void Reverse()
        {
            _value.Reverse();
            Notify();
        }

        public void Reverse(int index, int count)
        {
            _value.Reverse(index, count);
            Notify();
        }

        public void Sort()
        {
            _value.Sort();
            Notify();
        }

        public void Sort(Comparison<T> comparison)
        {
            _value.Sort(comparison);
            Notify();
        }

        public void Sort(IComparer<T> comparer)
        {
            _value.Sort(comparer);
            Notify();
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return _value.GetEnumerator();
        }

        public T this[int i]
        {
            get => _value[i];
            set
            {
                _value[i] = value;
                Notify();
            }
        }
    }
}