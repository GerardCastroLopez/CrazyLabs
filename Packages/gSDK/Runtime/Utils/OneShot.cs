using System;

namespace Utils
{
    public class OneShot
    {
        private Action _callback;

        
        public OneShot(Action callback)
        {
            _callback = callback;
        }

        public void Invoke()
        {
            var cb = _callback;
            _callback = null;
            cb?.Invoke();
        }
    }

    public class OneShot<T>
    {
        private Action<T> _callback;

        
        public OneShot(Action<T> callback)
        {
            _callback = callback;
        }

        public void Invoke(T arg)
        {
            var cb = _callback;
            _callback = null;
            cb?.Invoke(arg);
        }
    }
}