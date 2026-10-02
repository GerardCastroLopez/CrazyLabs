using System;
using System.Collections.Generic;
using UnityEngine;

namespace gSDK
{
    /// <summary>
    /// In order to serialise a Reactive value, either to display in the inspector or to store it in a json, you'll need to create a class extending Reactive with your type.
    /// </summary>
    public class Reactive<T>
    {
        private List<Action<T>> _listeners = new List<Action<T>>();

        [SerializeField] protected T _value;
        public T Value
        {
            get => _value;
            set
            {
                if(_value == null || !_value.Equals(value))
                {
                    ForceValue(value);
                }
            }
        }

        public Reactive() : this(default) { }

        public Reactive(T value)
        {
            _value = value;
        }

        public void ForceValue(T value)
        {
            _value = value;
            Notify();
        }

        public void AddListener(Action<T> callback, bool notifyImmediate = false)
        {
            if(_listeners.AddIfMissing(callback))
            {
                if(notifyImmediate)
                {
                    callback(_value);
                }
            }
        }

        public void RemoveListener(object caller)
        {
            _listeners.RemoveAll(l => l.Target == caller);
        }

        public void RemoveListener(Action<T> callback)
        {
            if(_listeners.Contains(callback))
            {
                _listeners.Remove(callback);
            }
        }

        public void RemoveAllListeners()
        {
            _listeners.Clear();
        }

        protected void Notify()
        {
            var toRemove = new List<int>();
            for(int i = _listeners.Count -1; i >= 0; --i)
            {
                var listener = _listeners[i];

                if(listener == null)
                {
                    toRemove.Add(i);
                }
                else
                {
                    _listeners[i](_value);
                }
            }

            foreach(var index in toRemove)
            {
                _listeners.RemoveAt(index);
            }
        }

        public static implicit operator Reactive<T>(T value)
        {
            return new(value);
        }
    }
}